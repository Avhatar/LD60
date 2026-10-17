using System;

namespace NodeGraphAvr.Editor
{
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
