using System;

namespace NodeGraphAvr.Editor
{
    /// <summary>
    /// Declares which Graph subclass a GraphEnvelopeExtension serializes extras for.
    /// Discovered via TypeCache by GraphEnvelopeExtensionRegistry.
    /// </summary>
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
