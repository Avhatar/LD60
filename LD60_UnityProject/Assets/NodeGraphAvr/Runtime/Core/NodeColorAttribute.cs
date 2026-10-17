using System;

namespace NodeGraphAvr
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class NodeColorAttribute : Attribute
    {
        public string HexColor { get; }

        public NodeColorAttribute(string hexColor)
        {
            HexColor = hexColor;
        }
    }
}
