using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace NodeGraphAvr.Editor
{
    /// <summary>
    /// Maps node types to their canvas view classes. Views declare their target with
    /// [CustomNodeView(typeof(X))] and are discovered via TypeCache; resolution walks
    /// the node's base-type chain so a view for a base class covers its descendants.
    /// Node types without any registered view get DefaultNodeView.
    /// </summary>
    public static class NodeViewRegistry
    {
        private static Dictionary<Type, Type> _declaredViews;
        private static readonly Dictionary<Type, Type> _resolvedViews = new Dictionary<Type, Type>();

        public static NodeView CreateView(Node node, GraphEditorWindow owner)
        {
            if (node == null)
            {
                Debug.LogError($"{nameof(NodeViewRegistry)}: {nameof(node)} is null in {nameof(CreateView)}. " +
                               "A view can only be created for an existing node. Fix: pass a node obtained from the edited graph.");
                return null;
            }

            var view = (NodeView)Activator.CreateInstance(ResolveViewType(node.GetType()));
            view.Init(node, owner);
            return view;
        }

        private static Type ResolveViewType(Type nodeType)
        {
            if (_resolvedViews.TryGetValue(nodeType, out var resolved))
                return resolved;

            EnsureDeclaredViews();
            resolved = typeof(DefaultNodeView);
            for (var type = nodeType; type != null && typeof(Node).IsAssignableFrom(type); type = type.BaseType)
            {
                if (_declaredViews.TryGetValue(type, out var declared))
                {
                    resolved = declared;
                    break;
                }
            }

            _resolvedViews.Add(nodeType, resolved);
            return resolved;
        }

        private static void EnsureDeclaredViews()
        {
            if (_declaredViews != null)
                return;

            _declaredViews = new Dictionary<Type, Type>();
            foreach (var viewType in TypeCache.GetTypesWithAttribute<CustomNodeViewAttribute>())
            {
                if (!typeof(NodeView).IsAssignableFrom(viewType) || viewType.IsAbstract)
                {
                    Debug.LogError($"{nameof(NodeViewRegistry)}: '{viewType.Name}' carries [{nameof(CustomNodeViewAttribute)}] but is not a concrete {nameof(NodeView)}. " +
                                   "The attribute only belongs on instantiable NodeView subclasses. Fix: derive from NodeView or remove the attribute.");
                    continue;
                }

                var attribute = (CustomNodeViewAttribute)Attribute.GetCustomAttribute(viewType, typeof(CustomNodeViewAttribute));
                if (attribute.NodeType == null || !typeof(Node).IsAssignableFrom(attribute.NodeType))
                {
                    Debug.LogError($"{nameof(NodeViewRegistry)}: '{viewType.Name}' targets '{attribute.NodeType?.Name ?? "null"}', which is not a {nameof(Node)} type. " +
                                   "Fix: point the attribute at a node class.");
                    continue;
                }

                if (_declaredViews.TryGetValue(attribute.NodeType, out var existing))
                {
                    Debug.LogError($"{nameof(NodeViewRegistry)}: both '{existing.Name}' and '{viewType.Name}' claim node type '{attribute.NodeType.Name}'. " +
                                   $"Keeping '{existing.Name}'. Fix: remove one of the two views.");
                    continue;
                }

                _declaredViews.Add(attribute.NodeType, viewType);
            }
        }
    }
}
