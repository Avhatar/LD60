using System;

namespace NodeGraphAvr.Editor
{
    /// <summary>
    /// Declares which source-node type a ConnectionInspector edits connections for.
    /// Discovered via TypeCache by ConnectionInspectorRegistry.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class CustomConnectionInspectorAttribute : Attribute
    {
        public Type SourceNodeType { get; }

        public CustomConnectionInspectorAttribute(Type sourceNodeType)
        {
            SourceNodeType = sourceNodeType;
        }
    }
}
