using System;

namespace NodeGraphAvr
{
    /// <summary>
    /// Declares the stable persistence id of a Graph or Node type. The JSON layer
    /// writes this id instead of the C# type name, so renaming a class never breaks
    /// saved graphs — the id stays, only the mapping changes. Ids must be unique
    /// across the project.
    /// </summary>
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
