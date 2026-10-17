using System;

namespace NodeGraphAvr.Editor
{
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
