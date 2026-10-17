using System;

namespace NodeGraphAvr
{
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
