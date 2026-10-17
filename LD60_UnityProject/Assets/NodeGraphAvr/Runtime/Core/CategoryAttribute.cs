using System;

namespace NodeGraphAvr
{
    /// <summary>
    /// Menu category of a node type — a slash-separated path that becomes nested
    /// folders in the creation browser. Without it the type sits at the root of the
    /// browser.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class CategoryAttribute : Attribute
    {
        public string Category { get; }

        public CategoryAttribute(string category)
        {
            Category = category;
        }
    }
}
