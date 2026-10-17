using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace NodeGraphAvr.Editor
{
    public static class GraphFieldMapper
    {
        private static readonly Dictionary<Type, List<FieldInfo>> _fieldCache = new Dictionary<Type, List<FieldInfo>>();

        public static bool HasMappedFields(Type type) => GetFields(type).Count > 0;

        public static void WriteFields(JsonWriter writer, object instance)
        {
            var type = instance.GetType();
            foreach (var field in GetFields(type))
                WriteField(writer, KeyFor(field), field.FieldType, field.GetValue(instance), type, field.Name, IsPolymorphic(field));
        }

        public static void ReadFields(JsonValue data, object instance)
        {
            var type = instance.GetType();
            foreach (var field in GetFields(type))
            {
                var value = data.Get(KeyFor(field));
                if (value != null)
                    field.SetValue(instance, ReadValue(field.FieldType, value, type, field.Name, IsPolymorphic(field)));
            }
        }

        private static bool IsPolymorphic(FieldInfo field) => field.IsDefined(typeof(SerializeReference), false);

        private static List<FieldInfo> GetFields(Type type)
        {
            if (_fieldCache.TryGetValue(type, out var cached))
                return cached;

            var result = new List<FieldInfo>();
            var boundary = typeof(Node).IsAssignableFrom(type) ? typeof(Node) : typeof(object);
            CollectFields(type, boundary, result);
            _fieldCache[type] = result;
            return result;
        }

        private static void CollectFields(Type type, Type boundary, List<FieldInfo> result)
        {
            if (type == null || type == boundary || type == typeof(object))
                return;

            CollectFields(type.BaseType, boundary, result);
            var declared = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            Array.Sort(declared, CompareByToken);
            foreach (var field in declared)
            {
                if (field.IsInitOnly || field.IsNotSerialized)
                    continue;
                if (!field.IsPublic && !field.IsDefined(typeof(SerializeField), false) && !IsPolymorphic(field))
                    continue;
                result.Add(field);
            }
        }

        private static int CompareByToken(FieldInfo a, FieldInfo b) => a.MetadataToken.CompareTo(b.MetadataToken);

        private static string KeyFor(FieldInfo field)
        {
            var name = field.Name;
            if (name.StartsWith("<", StringComparison.Ordinal))
            {
                var end = name.IndexOf('>');
                return end > 1 ? name.Substring(1, end - 1) : name;
            }

            return name.TrimStart('_');
        }

        private static void WriteField(JsonWriter writer, string key, Type type, object value, Type ownerType, string fieldName, bool polymorphic)
        {
            if (polymorphic)
            {
                WritePolymorphicField(writer, key, type, value, ownerType, fieldName);
                return;
            }

            if (type.IsAbstract)
                throw NeedsSerializeReference(type, ownerType, fieldName);

            if (type == typeof(int))
                writer.Property(key, (int)value);
            else if (type == typeof(long))
                writer.Property(key, (long)value);
            else if (type == typeof(float))
                writer.Property(key, (float)value);
            else if (type == typeof(double))
                writer.Property(key, (double)value);
            else if (type == typeof(bool))
                writer.Property(key, (bool)value);
            else if (type == typeof(string))
                writer.Property(key, (string)value ?? "");
            else if (type.IsEnum)
                writer.Property(key, value.ToString());
            else if (type == typeof(Vector2))
            {
                var v = (Vector2)value;
                writer.PropertyInlineFloats(key, new[] { v.x, v.y });
            }
            else if (type == typeof(Vector3))
            {
                var v = (Vector3)value;
                writer.PropertyInlineFloats(key, new[] { v.x, v.y, v.z });
            }
            else if (IsSupportedList(type, out var elementType))
            {
                writer.BeginArray(key);
                if (value != null)
                    foreach (var item in (IList)value)
                        WriteListElement(writer, elementType, item, ownerType, fieldName);
                writer.EndArray();
            }
            else if (IsAssetReference(type))
            {
                WriteAssetReference(writer, key, (UnityEngine.Object)value, ownerType, fieldName);
            }
            else if (IsNestedClass(type))
            {
                if (value == null)
                {
                    writer.PropertyNull(key);
                }
                else
                {
                    writer.BeginObject(key);
                    WriteFields(writer, value);
                    writer.EndObject();
                }
            }
            else
            {
                throw Unsupported(type, ownerType, fieldName);
            }
        }

        private static void WritePolymorphicField(JsonWriter writer, string key, Type type, object value, Type ownerType, string fieldName)
        {
            if (IsSupportedList(type, out _))
            {
                var list = (IList)value;
                if (list == null || list.Count == 0)
                    return;

                writer.BeginArray(key);
                for (var i = 0; i < list.Count; i++)
                    WritePolymorphicValue(writer, list[i], ownerType, fieldName);
                writer.EndArray();
                return;
            }

            if (value == null)
                return;

            writer.BeginObject(key);
            WritePolymorphicBody(writer, value, ownerType, fieldName);
            writer.EndObject();
        }

        private static void WritePolymorphicValue(JsonWriter writer, object value, Type ownerType, string fieldName)
        {
            if (value == null)
            {
                writer.ValueNull();
                return;
            }

            writer.BeginObject();
            WritePolymorphicBody(writer, value, ownerType, fieldName);
            writer.EndObject();
        }

        private static void WritePolymorphicBody(JsonWriter writer, object value, Type ownerType, string fieldName)
        {
            var concreteType = value.GetType();
            writer.Property("type", GraphTypeRegistry.GetIdOrThrow(concreteType));
            if (!HasMappedFields(concreteType))
                return;

            writer.BeginObject("data");
            WriteFields(writer, value);
            writer.EndObject();
        }

        private static object ReadPolymorphicField(Type type, JsonValue value, Type ownerType, string fieldName)
        {
            if (IsSupportedList(type, out var elementType))
            {
                var list = (IList)Activator.CreateInstance(type);
                if (value.Kind == JsonKind.Array)
                    foreach (var item in value.Array)
                        list.Add(ReadPolymorphicValue(elementType, item, ownerType, fieldName));
                return list;
            }

            return ReadPolymorphicValue(type, value, ownerType, fieldName);
        }

        private static object ReadPolymorphicValue(Type expectedType, JsonValue value, Type ownerType, string fieldName)
        {
            if (value == null || value.Kind == JsonKind.Null)
                return null;

            var id = value.GetString("type");
            if (!GraphTypeRegistry.TryGetType(id, out var concreteType))
            {
                Debug.LogError($"{nameof(GraphFieldMapper)}: field '{fieldName}' on '{ownerType.Name}' references unknown type id '{id}'. " +
                               "The value was read as null and everything else in the graph still loaded. " +
                               "Fix: restore the script declaring that [GraphTypeId], or re-author the field and save.");
                return null;
            }

            if (!expectedType.IsAssignableFrom(concreteType))
            {
                Debug.LogError($"{nameof(GraphFieldMapper)}: field '{fieldName}' on '{ownerType.Name}' expects a '{expectedType.Name}' " +
                               $"but type id '{id}' resolves to '{concreteType.Name}'. The value was read as null. " +
                               "Fix: correct the type id in the file, or re-author the field and save.");
                return null;
            }

            if (concreteType.GetConstructor(Type.EmptyTypes) == null)
            {
                Debug.LogError($"{nameof(GraphFieldMapper)}: type '{concreteType.Name}' (id '{id}') has no parameterless constructor, " +
                               $"so field '{fieldName}' on '{ownerType.Name}' was read as null. " +
                               "Fix: give the type a public parameterless constructor — graph data is created by the serializer.");
                return null;
            }

            var instance = Activator.CreateInstance(concreteType);
            var data = value.Get("data");
            if (data != null)
                ReadFields(data, instance);
            return instance;
        }

        private static void WriteAssetReference(JsonWriter writer, string key, UnityEngine.Object asset, Type ownerType, string fieldName)
        {
            if (asset == null)
            {
                writer.PropertyNull(key);
                return;
            }

            writer.BeginObject(key);
            WriteAssetReferenceBody(writer, asset, $"field '{fieldName}' on '{ownerType.Name}'");
            writer.EndObject();
        }

        public static void WriteAssetReferenceValue(JsonWriter writer, UnityEngine.Object asset, string context)
        {
            writer.BeginObject();
            WriteAssetReferenceBody(writer, asset, context);
            writer.EndObject();
        }

        private static void WriteAssetReferenceBody(JsonWriter writer, UnityEngine.Object asset, string context)
        {
            if (!UnityEditor.AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out var guid, out long _) || string.IsNullOrEmpty(guid))
                throw new NotSupportedException(
                    $"{nameof(GraphFieldMapper)}: {context} references '{asset.name}', " +
                    "which is not a persisted asset. Only project assets can be referenced from graph JSON. " +
                    "Fix: assign an asset created on disk (Create menu), not an in-memory instance.");

            writer.Property("guid", guid);
            writer.Property("name", asset.name);
        }

        private static void WriteListElement(JsonWriter writer, Type elementType, object item, Type ownerType, string fieldName)
        {
            if (elementType.IsAbstract)
                throw NeedsSerializeReference(elementType, ownerType, fieldName);

            if (elementType == typeof(int))
                writer.Value((int)item);
            else if (elementType == typeof(long))
                writer.Value((long)item);
            else if (elementType == typeof(float))
                writer.Value((float)item);
            else if (elementType == typeof(double))
                writer.Value((double)item);
            else if (elementType == typeof(bool))
                writer.Value((bool)item);
            else if (elementType == typeof(string))
                writer.Value((string)item ?? "");
            else if (elementType.IsEnum)
                writer.Value(item.ToString());
            else if (IsNestedClass(elementType))
            {
                if (item == null)
                {
                    writer.ValueNull();
                }
                else
                {
                    writer.BeginObject();
                    WriteFields(writer, item);
                    writer.EndObject();
                }
            }
            else
            {
                throw Unsupported(elementType, ownerType, fieldName);
            }
        }

        private static object ReadValue(Type type, JsonValue value, Type ownerType, string fieldName, bool polymorphic = false)
        {
            if (polymorphic)
                return ReadPolymorphicField(type, value, ownerType, fieldName);

            if (type.IsAbstract)
                throw NeedsSerializeReference(type, ownerType, fieldName);

            if (type == typeof(int))
                return (int)value.Number;
            if (type == typeof(long))
                return (long)value.Number;
            if (type == typeof(float))
                return (float)value.Number;
            if (type == typeof(double))
                return value.Number;
            if (type == typeof(bool))
                return value.Bool;
            if (type == typeof(string))
                return value.String;
            if (type.IsEnum)
                return Enum.Parse(type, value.String);
            if (type == typeof(Vector2))
                return new Vector2((float)value.Array[0].Number, (float)value.Array[1].Number);
            if (type == typeof(Vector3))
                return new Vector3((float)value.Array[0].Number, (float)value.Array[1].Number, (float)value.Array[2].Number);
            if (IsSupportedList(type, out var elementType))
            {
                var list = (IList)Activator.CreateInstance(type);
                foreach (var item in value.Array)
                    list.Add(ReadValue(elementType, item, ownerType, fieldName));
                return list;
            }

            if (IsAssetReference(type))
                return ReadAssetReference(type, value, ownerType, fieldName);

            if (IsNestedClass(type))
            {
                if (value.Kind == JsonKind.Null)
                    return null;
                var nested = Activator.CreateInstance(type);
                ReadFields(value, nested);
                return nested;
            }

            throw Unsupported(type, ownerType, fieldName);
        }

        private static object ReadAssetReference(Type type, JsonValue value, Type ownerType, string fieldName)
        {
            return ReadAssetReferenceValue(type, value, $"field '{fieldName}' on '{ownerType.Name}'");
        }

        public static UnityEngine.Object ReadAssetReferenceValue(Type type, JsonValue value, string context)
        {
            if (value.Kind == JsonKind.Null)
                return null;

            var guid = value.GetString("guid");
            if (string.IsNullOrEmpty(guid))
            {
                Debug.LogError($"{nameof(GraphFieldMapper)}: {context} has an asset " +
                               "reference without a guid in JSON. The reference was read as null. " +
                               "Fix: restore the guid key or re-assign the asset in the editor and save.");
                return null;
            }

            var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            var asset = string.IsNullOrEmpty(path) ? null : UnityEditor.AssetDatabase.LoadAssetAtPath(path, type);
            if (asset == null)
                Debug.LogError($"{nameof(GraphFieldMapper)}: {context} references asset " +
                               $"guid '{guid}' ('{value.GetString("name", "?")}'), which does not resolve to a {type.Name}. " +
                               "The reference was read as null. Fix: restore the missing asset or re-assign the field and save.");
            return asset;
        }

        private static bool IsSupportedList(Type type, out Type elementType)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            {
                elementType = type.GetGenericArguments()[0];
                return true;
            }

            elementType = null;
            return false;
        }

        private static bool IsAssetReference(Type type)
        {
            return typeof(ScriptableObject).IsAssignableFrom(type);
        }

        private static bool IsNestedClass(Type type)
        {
            return type.IsClass
                   && type != typeof(string)
                   && !type.IsArray
                   && !typeof(UnityEngine.Object).IsAssignableFrom(type)
                   && type.IsDefined(typeof(SerializableAttribute), false);
        }

        private static NotSupportedException Unsupported(Type type, Type ownerType, string fieldName)
        {
            return new NotSupportedException(
                $"{nameof(GraphFieldMapper)}: field '{fieldName}' on '{ownerType.Name}' has unsupported type '{type.Name}'. " +
                "Supported: int, long, float, double, bool, string, enums, Vector2/Vector3, List of those (except vectors), " +
                "nested [Serializable] classes, ScriptableObject asset references, and polymorphic [SerializeReference] " +
                "fields or lists of types carrying [GraphTypeId]. " +
                $"Fix: change the field type or extend {nameof(GraphFieldMapper)} deliberately.");
        }

        private static NotSupportedException NeedsSerializeReference(Type type, Type ownerType, string fieldName)
        {
            return new NotSupportedException(
                $"{nameof(GraphFieldMapper)}: field '{fieldName}' on '{ownerType.Name}' has abstract type '{type.Name}' " +
                "but is not marked [SerializeReference], so neither Unity nor this serializer could keep the concrete subtype. " +
                "Fix: add [SerializeReference] to the field — that is what makes a field polymorphic here.");
        }
    }
}
