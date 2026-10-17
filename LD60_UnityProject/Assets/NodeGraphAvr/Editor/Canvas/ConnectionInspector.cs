using UnityEditor;

namespace NodeGraphAvr.Editor
{
    /// <summary>
    /// Extension point letting a graph system draw editable, connection-specific
    /// fields in the inspector panel when a connection is selected — resolved by the
    /// connection's source node type. The panel supplies a live SerializedObject over
    /// the working-copy graph; implementations edit through property paths so undo
    /// and dirty tracking come from the panel's apply/flush cycle.
    /// </summary>
    public abstract class ConnectionInspector
    {
        public abstract void OnGUI(SerializedObject graphObject, Connection connection);
    }
}
