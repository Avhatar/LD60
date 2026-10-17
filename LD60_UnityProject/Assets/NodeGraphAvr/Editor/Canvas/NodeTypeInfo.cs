using System;
using UnityEngine;

namespace NodeGraphAvr.Editor
{
    /// <summary>
    /// Cached authoring metadata of one node, action or condition type — display name, menu
    /// category, description, and the tint **declared in code** — resolved once from the
    /// type's attributes. The canvas reads it on the draw path, so resolution must never
    /// repeat per repaint. What the canvas actually draws with comes from the two static
    /// resolvers below, which layer the project's NodeStyleConfig over these defaults.
    /// </summary>
    public class NodeTypeInfo
    {
        private static readonly System.Collections.Generic.Dictionary<Type, NodeTypeInfo> _cache =
            new System.Collections.Generic.Dictionary<Type, NodeTypeInfo>();

        public string Name { get; private set; }

        public string Category { get; private set; }

        public string Description { get; private set; }

        public string MenuPath { get; private set; }

        /// <summary>
        /// What a type draws with when neither code nor the project says otherwise. There is no
        /// such thing as a colorless node: a type without [NodeColor] uses this.
        /// </summary>
        public static readonly Color FallbackColor = new Color(0.541f, 0.541f, 0.565f, 1f);

        /// <summary>True when the type declares its own [NodeColor] in code.</summary>
        public bool HasColor { get; private set; }

        /// <summary>
        /// The type's default color: what its [NodeColor] declares, or the fallback when it
        /// declares none. Always a usable color; a project override is layered on top of it by
        /// the resolvers below, never here.
        /// </summary>
        public Color Color { get; private set; }

        /// <summary>
        /// Color this type claims as its own — a project override, or a declared [NodeColor].
        /// False means the type expresses no preference and takes the fallback, which is what
        /// lets an assigned action's color win over a host node that has an opinion while a
        /// colorless action leaves that opinion alone.
        /// </summary>
        public static bool TryGetOwnColor(Type type, out Color color)
        {
            if (NodeStyleCache.TryGetColor(type, out color))
                return true;

            var info = For(type);
            color = info.Color;
            return info.HasColor;
        }

        /// <summary>
        /// Effective color of one type: its override, its declared color, or the fallback. Every
        /// lookup is cached, so this stays two dictionary lookups on the draw path.
        /// </summary>
        public static Color GetTypeColor(Type type)
        {
            return TryGetOwnColor(type, out var color) ? color : FallbackColor;
        }

        /// <summary>
        /// Resolves the color a node draws with: an assigned action's own color wins, so an
        /// action node looks like what it does; otherwise the node type's own color; otherwise
        /// the fallback. Overrides are honored on either side.
        /// </summary>
        public static Color GetColor(Node node)
        {
            if (node == null)
            {
                Debug.LogError($"{nameof(NodeTypeInfo)}: {nameof(node)} is null in {nameof(GetColor)}. " +
                               "A node is required to resolve the color it draws with. Fix: skip null entries before drawing.");
                return FallbackColor;
            }

            if (node is IActionHost host && host.Action != null && TryGetOwnColor(host.Action.GetType(), out var actionColor))
                return actionColor;

            return GetTypeColor(node.GetType());
        }

        public static NodeTypeInfo For(Type type)
        {
            if (type == null)
            {
                Debug.LogError($"{nameof(NodeTypeInfo)}: {nameof(type)} is null in {nameof(For)}. " +
                               "A node type is required to resolve its menu metadata. Fix: pass the node's concrete type.");
                return new NodeTypeInfo { Name = "", Category = "", Description = "", MenuPath = "" };
            }

            if (_cache.TryGetValue(type, out var info))
                return info;

            info = Resolve(type);
            _cache.Add(type, info);
            return info;
        }

        private static NodeTypeInfo Resolve(Type type)
        {
            var categoryAttribute = (CategoryAttribute)Attribute.GetCustomAttribute(type, typeof(CategoryAttribute));
            var descriptionAttribute = (DescriptionAttribute)Attribute.GetCustomAttribute(type, typeof(DescriptionAttribute));
            var colorAttribute = (NodeColorAttribute)Attribute.GetCustomAttribute(type, typeof(NodeColorAttribute));

            var info = new NodeTypeInfo
            {
                Name = Node.DisplayNameOf(type),
                Category = categoryAttribute != null ? (categoryAttribute.Category ?? "").Trim('/') : "",
                Description = descriptionAttribute != null ? descriptionAttribute.Description ?? "" : "",
                Color = FallbackColor,
            };

            info.MenuPath = string.IsNullOrEmpty(info.Category) ? info.Name : info.Category + "/" + info.Name;

            if (colorAttribute != null)
            {
                var hex = colorAttribute.HexColor ?? "";
                if (!hex.StartsWith("#", StringComparison.Ordinal))
                    hex = "#" + hex;
                if (ColorUtility.TryParseHtmlString(hex, out var color))
                {
                    info.HasColor = true;
                    info.Color = color;
                }
                else
                {
                    Debug.LogError($"{nameof(NodeTypeInfo)}: '{type.Name}' declares [{nameof(NodeColorAttribute)}] with unparsable hex '{colorAttribute.HexColor}'. " +
                                   "The node draws in the default color. Fix: use \"RRGGBB\" or \"#RRGGBB\".");
                }
            }

            return info;
        }
    }
}
