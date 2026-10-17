using System;
using System.Collections.Generic;
using UnityEngine;

namespace NodeGraphAvr
{
    [Serializable]
    public abstract class Node
    {
        [Tooltip("Stable identity of the node inside its graph file. Created once; survives renames and moves. Connections and the JSON layer reference nodes by this id.")]
        [SerializeField] private string _uid;

        [Tooltip("Canvas position of the node. Authoring data only — runtime execution must never depend on it.")]
        [SerializeField] private Vector2 _position;

        [Tooltip("Canvas size of the node. The author resizes a node to fit as much of its body text as wanted; authoring data only.")]
        [SerializeField] private Vector2 _size = DefaultSize;

        [Tooltip("Author comment shown next to the node on the canvas. Purely informational.")]
        [SerializeField] private string _comment;

        [Tooltip("Optional display name overriding the type-derived one on the canvas. Empty means the type name is shown.")]
        [SerializeField] private string _customName;

        [Tooltip("Outgoing connections in child order. The order is meaningful: for tree graphs it is the execution order of children.")]
        [SerializeReference] private List<Connection> _outConnections = new List<Connection>();

        [NonSerialized] private Graph _graph;
        [NonSerialized] private List<Connection> _inConnections;

        public string Uid => string.IsNullOrEmpty(_uid) ? _uid = Guid.NewGuid().ToString("N") : _uid;

        public Graph Graph
        {
            get { return _graph; }
            internal set { _graph = value; }
        }

        public static readonly Vector2 DefaultSize = new Vector2(160f, 40f);

        public static readonly Vector2 MinSize = new Vector2(80f, 36f);

        public Vector2 Position
        {
            get { return _position; }
            internal set { _position = value; }
        }

        public Vector2 Size
        {
            get { return _size; }
            internal set { _size = value; }
        }

        public string Comment
        {
            get { return _comment; }
            internal set { _comment = value; }
        }

        public string CustomName
        {
            get { return _customName; }
            internal set { _customName = value; }
        }

        public string Name
        {
            get
            {
                if (string.IsNullOrEmpty(_customName))
                    return DisplayNameOf(GetType());

                if (!ReferenceEquals(_cachedNameSource, _customName))
                {
                    _cachedNameSource = _customName;
                    _cachedName = DisplayNameOf(GetType()) + " - " + _customName;
                }

                return _cachedName;
            }
        }

        [NonSerialized] private string _cachedName;
        [NonSerialized] private string _cachedNameSource;

        private static readonly Dictionary<Type, string> _displayNames = new Dictionary<Type, string>();

        public static string DisplayNameOf(Type nodeType)
        {
            if (nodeType == null)
            {
                Debug.LogError($"{nameof(Node)}: {nameof(nodeType)} is null in {nameof(DisplayNameOf)}. " +
                               "A concrete type is required to resolve its display name. Fix: pass the instance's GetType().");
                return "";
            }

            if (_displayNames.TryGetValue(nodeType, out var displayName))
                return displayName;

            var attribute = (NameAttribute)Attribute.GetCustomAttribute(nodeType, typeof(NameAttribute));
            displayName = attribute != null && !string.IsNullOrEmpty(attribute.Name) ? attribute.Name : nodeType.Name;
            _displayNames.Add(nodeType, displayName);
            return displayName;
        }

        public IReadOnlyList<Connection> InConnections => InList;

        public IReadOnlyList<Connection> OutConnections => OutList;

        internal List<Connection> InList => _inConnections ??= new List<Connection>();

        internal List<Connection> OutList => _outConnections ??= new List<Connection>();

        public abstract int MaxInConnections { get; }

        public abstract int MaxOutConnections { get; }

        public virtual Type OutConnectionType => typeof(Connection);

        public virtual bool AllowAsStart => true;

        protected internal virtual bool CanConnectTo(Node target) => true;

        protected internal virtual bool CanConnectFrom(Node source) => true;

        protected internal virtual void OnRuntimeStateRebuilt()
        {
        }

        internal void RestorePersistentState(string uid, Vector2 position, Vector2 size, string comment, string customName)
        {
            _uid = uid;
            _position = position;
            _size = size;
            _comment = comment;
            _customName = customName;
        }
    }
}
