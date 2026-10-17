using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace NodeGraphAvr.Editor
{
    public static class ConnectionInspectorRegistry
    {
        private static Dictionary<Type, ConnectionInspector> _declared;
        private static readonly Dictionary<Type, ConnectionInspector> _resolved = new Dictionary<Type, ConnectionInspector>();

        public static ConnectionInspector Resolve(Type sourceNodeType)
        {
            if (sourceNodeType == null)
                return null;

            if (_resolved.TryGetValue(sourceNodeType, out var cached))
                return cached;

            EnsureDeclared();
            ConnectionInspector resolved = null;
            for (var type = sourceNodeType; type != null && typeof(Node).IsAssignableFrom(type); type = type.BaseType)
            {
                if (_declared.TryGetValue(type, out var declared))
                {
                    resolved = declared;
                    break;
                }
            }

            _resolved.Add(sourceNodeType, resolved);
            return resolved;
        }

        private static void EnsureDeclared()
        {
            if (_declared != null)
                return;

            _declared = new Dictionary<Type, ConnectionInspector>();
            foreach (var inspectorType in TypeCache.GetTypesWithAttribute<CustomConnectionInspectorAttribute>())
            {
                if (!typeof(ConnectionInspector).IsAssignableFrom(inspectorType) || inspectorType.IsAbstract)
                {
                    Debug.LogError($"{nameof(ConnectionInspectorRegistry)}: '{inspectorType.Name}' carries [{nameof(CustomConnectionInspectorAttribute)}] but is not a concrete {nameof(ConnectionInspector)}. " +
                                   "Fix: derive from ConnectionInspector or remove the attribute.");
                    continue;
                }

                var attribute = (CustomConnectionInspectorAttribute)Attribute.GetCustomAttribute(inspectorType, typeof(CustomConnectionInspectorAttribute));
                if (attribute.SourceNodeType == null || !typeof(Node).IsAssignableFrom(attribute.SourceNodeType))
                {
                    Debug.LogError($"{nameof(ConnectionInspectorRegistry)}: '{inspectorType.Name}' targets '{attribute.SourceNodeType?.Name ?? "null"}', which is not a {nameof(Node)} type. " +
                                   "Fix: point the attribute at a node class.");
                    continue;
                }

                if (_declared.TryGetValue(attribute.SourceNodeType, out var existing))
                {
                    Debug.LogError($"{nameof(ConnectionInspectorRegistry)}: both '{existing.GetType().Name}' and '{inspectorType.Name}' claim node type '{attribute.SourceNodeType.Name}'. " +
                                   $"Keeping '{existing.GetType().Name}'. Fix: remove one of the two inspectors.");
                    continue;
                }

                _declared.Add(attribute.SourceNodeType, (ConnectionInspector)Activator.CreateInstance(inspectorType));
            }
        }
    }
}
