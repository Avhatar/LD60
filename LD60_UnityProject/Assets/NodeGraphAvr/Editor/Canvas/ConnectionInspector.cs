using UnityEditor;

namespace NodeGraphAvr.Editor
{
    public abstract class ConnectionInspector
    {
        public abstract void OnGUI(SerializedObject graphObject, Connection connection);
    }
}
