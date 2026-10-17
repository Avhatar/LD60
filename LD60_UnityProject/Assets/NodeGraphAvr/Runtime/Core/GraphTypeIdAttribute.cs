using System;

namespace NodeGraphAvr
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class GraphTypeIdAttribute : Attribute
    {
        public string Id { get; }

        public GraphTypeIdAttribute(string id)
        {
            Id = id;
        }
    }
}
