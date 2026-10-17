using System;
using System.Collections.Generic;
using UnityEngine;

namespace NodeGraphAvr
{
    /// <summary>
    /// A directed link between two nodes of the same graph. Plain serializable class,
    /// persisted inside the source node's out-list; endpoints are shared references.
    /// Carries no behavior of its own in the MVP — subclasses may add per-edge data later.
    /// </summary>
    [Serializable]
    public class Connection
    {
        [Tooltip("Stable identity of the connection inside its graph file. Created once; the JSON layer references connections by this id.")]
        [SerializeField] private string _uid;

        [Tooltip("Node this connection leaves from. Owned by that node's out-list.")]
        [SerializeReference] private Node _source;

        [Tooltip("Node this connection arrives at.")]
        [SerializeReference] private Node _target;

        [Tooltip("Intermediate points the connection line passes through on the canvas, in canvas space and source-to-target order. Authoring data only.")]
        [SerializeField] private List<Vector2> _waypoints = new List<Vector2>();

        [Tooltip("When set, the line bends sharply at each intermediate point instead of curving smoothly through it.")]
        [SerializeField] private bool _sharpBends;

        public string Uid => string.IsNullOrEmpty(_uid) ? _uid = Guid.NewGuid().ToString("N") : _uid;

        public Node Source
        {
            get { return _source; }
            internal set { _source = value; }
        }

        public Node Target
        {
            get { return _target; }
            internal set { _target = value; }
        }

        public Graph Graph => _source != null ? _source.Graph : null;

        public IReadOnlyList<Vector2> Waypoints => WaypointList;

        internal List<Vector2> WaypointList => _waypoints ??= new List<Vector2>();

        public bool SharpBends
        {
            get { return _sharpBends; }
            internal set { _sharpBends = value; }
        }

        internal void RestoreUid(string uid)
        {
            _uid = uid;
        }
    }
}
