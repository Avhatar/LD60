using System;

namespace NodeGraphAvr
{
    /// <summary>
    /// Tint of a node type on the canvas and its swatch in the creation browser, as a
    /// hex string ("RRGGBB" or "#RRGGBB"). Without it the node draws in the editor's
    /// default window color. Named NodeColor rather than Color so the attribute never
    /// competes with UnityEngine.Color at a usage site.
    /// </summary>
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
