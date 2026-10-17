using System;

namespace NodeGraphAvr.Editor
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class GraphEnvelopeExtensionAttribute : Attribute
    {
        public Type GraphType { get; }

        public GraphEnvelopeExtensionAttribute(Type graphType)
        {
            GraphType = graphType;
        }
    }
}
