using System;

namespace NodeGraphAvr
{
    /// <summary>
    /// Friendly display name of a node type, used by the editor wherever the type is
    /// shown to an author (creation browser, node header). Without it the C# type
    /// name is shown.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class NameAttribute : Attribute
    {
        public string Name { get; }

        public NameAttribute(string name)
        {
            Name = name;
        }
    }
}
