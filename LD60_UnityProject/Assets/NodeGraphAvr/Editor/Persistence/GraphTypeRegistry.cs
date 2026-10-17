using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace NodeGraphAvr.Editor
{
    /// <summary>
    /// Bidirectional map between stable persistence ids ([GraphTypeId]) and C# types,
    /// built lazily from TypeCache. The id is what graph files store instead of type
    /// names, making C# renames a non-event for saved data. Duplicate ids are
    /// reported as errors; the first registration wins.
    /// </summary>
    public static class GraphTypeRegistry
    {
        private static Dictionary<string, Type> _typeById;
        private static Dictionary<Type, string> _idByType;

        public static bool TryGetId(Type type, out string id)
        {
            EnsureBuilt();
            if (type != null)
                return _idByType.TryGetValue(type, out id);
            id = null;
            return false;
        }

        public static string GetIdOrThrow(Type type)
        {
            EnsureBuilt();
            if (_idByType.TryGetValue(type, out var id))
                return id;
            throw new NotSupportedException(
                $"{nameof(GraphTypeRegistry)}: type '{type.Name}' has no [{nameof(GraphTypeIdAttribute)}] and cannot be persisted. " +
                "Fix: add [GraphTypeId(\"stable-unique-id\")] to the class.");
        }

        public static bool TryGetType(string id, out Type type)
        {
            EnsureBuilt();
            if (!string.IsNullOrEmpty(id))
                return _typeById.TryGetValue(id, out type);
            type = null;
            return false;
        }

        private static void EnsureBuilt()
        {
            if (_typeById != null)
                return;

            _typeById = new Dictionary<string, Type>();
            _idByType = new Dictionary<Type, string>();
            foreach (var type in TypeCache.GetTypesWithAttribute<GraphTypeIdAttribute>())
            {
                var attributes = type.GetCustomAttributes(typeof(GraphTypeIdAttribute), false);
                var id = ((GraphTypeIdAttribute)attributes[0]).Id;
                if (string.IsNullOrEmpty(id))
                {
                    Debug.LogError($"{nameof(GraphTypeRegistry)}: type '{type.Name}' declares an empty [{nameof(GraphTypeIdAttribute)}] id. " +
                                   "The type is skipped and cannot be persisted. Fix: give it a stable unique id.");
                    continue;
                }

                if (_typeById.TryGetValue(id, out var existing))
                {
                    Debug.LogError($"{nameof(GraphTypeRegistry)}: duplicate [{nameof(GraphTypeIdAttribute)}] id '{id}' on '{type.Name}' — " +
                                   $"already used by '{existing.Name}'. The later type is skipped. Fix: give each type a unique id.");
                    continue;
                }

                _typeById.Add(id, type);
                _idByType.Add(type, id);
            }
        }
    }
}
