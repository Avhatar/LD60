using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace NodeGraphAvr.Editor
{
    public static class GraphEnvelopeExtensionRegistry
    {
        private static Dictionary<Type, GraphEnvelopeExtension> _declared;
        private static readonly Dictionary<Type, GraphEnvelopeExtension> _resolved = new Dictionary<Type, GraphEnvelopeExtension>();

        public static GraphEnvelopeExtension Resolve(Type graphType)
        {
            if (_resolved.TryGetValue(graphType, out var cached))
                return cached;

            EnsureDeclared();
            GraphEnvelopeExtension resolved = null;
            for (var type = graphType; type != null && typeof(Graph).IsAssignableFrom(type); type = type.BaseType)
            {
                if (_declared.TryGetValue(type, out var declared))
                {
                    resolved = declared;
                    break;
                }
            }

            _resolved.Add(graphType, resolved);
            return resolved;
        }

        private static void EnsureDeclared()
        {
            if (_declared != null)
                return;

            _declared = new Dictionary<Type, GraphEnvelopeExtension>();
            foreach (var extensionType in TypeCache.GetTypesWithAttribute<GraphEnvelopeExtensionAttribute>())
            {
                if (!typeof(GraphEnvelopeExtension).IsAssignableFrom(extensionType) || extensionType.IsAbstract)
                {
                    Debug.LogError($"{nameof(GraphEnvelopeExtensionRegistry)}: '{extensionType.Name}' carries [{nameof(GraphEnvelopeExtensionAttribute)}] but is not a concrete {nameof(GraphEnvelopeExtension)}. " +
                                   "Fix: derive from GraphEnvelopeExtension or remove the attribute.");
                    continue;
                }

                var attribute = (GraphEnvelopeExtensionAttribute)Attribute.GetCustomAttribute(extensionType, typeof(GraphEnvelopeExtensionAttribute));
                if (attribute.GraphType == null || !typeof(Graph).IsAssignableFrom(attribute.GraphType))
                {
                    Debug.LogError($"{nameof(GraphEnvelopeExtensionRegistry)}: '{extensionType.Name}' targets '{attribute.GraphType?.Name ?? "null"}', which is not a {nameof(Graph)} type. " +
                                   "Fix: point the attribute at a Graph class.");
                    continue;
                }

                if (_declared.TryGetValue(attribute.GraphType, out var existing))
                {
                    Debug.LogError($"{nameof(GraphEnvelopeExtensionRegistry)}: both '{existing.GetType().Name}' and '{extensionType.Name}' claim graph type '{attribute.GraphType.Name}'. " +
                                   $"Keeping '{existing.GetType().Name}'. Fix: remove one of the two extensions.");
                    continue;
                }

                _declared.Add(attribute.GraphType, (GraphEnvelopeExtension)Activator.CreateInstance(extensionType));
            }
        }
    }
}
