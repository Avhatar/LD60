using System;

namespace NodeGraphAvr.Editor
{
    /// <summary>
    /// Marks a NodeView subclass as the canvas view for a specific node type (and its
    /// descendants unless they declare a closer view). Discovered via TypeCache by
    /// NodeViewRegistry; node types without a matching view fall back to
    /// DefaultNodeView.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class CustomNodeViewAttribute : Attribute
    {
        public Type NodeType { get; }

        public CustomNodeViewAttribute(Type nodeType)
        {
            NodeType = nodeType;
        }
    }
}
