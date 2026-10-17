using System;
using UnityEngine;

namespace NodeGraphAvr.Editor
{
    public class NodeTypeInfo
    {
        private static readonly System.Collections.Generic.Dictionary<Type, NodeTypeInfo> _cache =
            new System.Collections.Generic.Dictionary<Type, NodeTypeInfo>();

        public string Name { get; private set; }

        public string Category { get; private set; }

        public string Description { get; private set; }

        public string MenuPath { get; private set; }

        public static readonly Color FallbackColor = new Color(0.541f, 0.541f, 0.565f, 1f);

        public bool HasColor { get; private set; }

        public Color Color { get; private set; }

        public static bool TryGetOwnColor(Type type, out Color color)
        {
            if (NodeStyleCache.TryGetColor(type, out color))
                return true;

            var info = For(type);
            color = info.Color;
            return info.HasColor;
        }

        public static Color GetTypeColor(Type type)
        {
            return TryGetOwnColor(type, out var color) ? color : FallbackColor;
        }

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
