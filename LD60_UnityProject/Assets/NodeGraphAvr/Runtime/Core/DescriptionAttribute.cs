using System;

namespace NodeGraphAvr
{
    /// <summary>
    /// One-paragraph explanation of what a node type does, shown in the creation
    /// browser's footer while the entry is hovered. Write it for the author picking
    /// the node, not for the programmer reading the class.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class DescriptionAttribute : Attribute
    {
        public string Description { get; }

        public DescriptionAttribute(string description)
        {
            Description = description;
        }
    }
}
