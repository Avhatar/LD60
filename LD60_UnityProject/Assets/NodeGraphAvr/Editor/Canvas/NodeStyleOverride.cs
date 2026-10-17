using UnityEngine;

namespace NodeGraphAvr.Editor
{
    [System.Serializable]
    public class NodeStyleOverride
    {
        [Tooltip("Stable [GraphTypeId] of the overridden node, action or condition type. An id that no longer resolves is reported in the config inspector instead of being dropped silently.")]
        [SerializeField] private string _typeId = "";

        [Tooltip("Color this type draws with, replacing the one its [NodeColor] attribute declares in code.")]
        [SerializeField] private Color _color = Color.white;

        public string TypeId
        {
            get { return _typeId; }
            internal set { _typeId = value; }
        }

        public Color Color
        {
            get { return _color; }
            internal set { _color = value; }
        }

        public NodeStyleOverride() { }

        public NodeStyleOverride(string typeId, Color color)
        {
            _typeId = typeId;
            _color = color;
        }
    }
}
