using System;
using UnityEditor;
using UnityEngine;

namespace NodeGraphAvr.Editor
{
    /// <summary>
    /// Draws one [SerializeReference] field: a header naming the assigned type (or its
    /// one-line self-description when it has one), a picker opening the searchable type
    /// browser, a remove button, and the assigned object's own fields below. Assignment
    /// goes through the graph's mutation seam so it lands as a single undo step and marks
    /// the session dirty; removal rides the owning panel's apply cycle. Shared by the
    /// action and condition drawers — nothing here knows what an action is.
    /// </summary>
    public static class PolymorphicReferenceField
    {
        private const float Spacing = 2f;
        private const float AssignButtonWidth = 88f;
        private const float ChangeButtonWidth = 24f;
        private const float RemoveButtonWidth = 20f;
        private const float ValidationHeight = 34f;
        private const float MissingAttributeHeight = 34f;

        private static readonly GUIContent _assignContent = new GUIContent("Assign…", "Pick a type for this slot.");
        private static readonly GUIContent _changeContent = new GUIContent("…", "Replace with another type.");
        private static readonly GUIContent _removeContent = new GUIContent("×", "Clear this slot.");
        private static readonly GUIContent _headerContent = new GUIContent();
        private static GUIStyle _headerStyle;

        private static GUIStyle HeaderStyle =>
            _headerStyle ??= new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleRight };

        public static float GetHeight(SerializedProperty property, string validationError)
        {
            if (property.propertyType != SerializedPropertyType.ManagedReference)
                return MissingAttributeHeight;

            var height = EditorGUIUtility.singleLineHeight;
            if (property.managedReferenceValue == null || !property.isExpanded)
                return height;

            var iterator = property.Copy();
            var end = iterator.GetEndProperty();
            if (iterator.NextVisible(true))
            {
                while (!SerializedProperty.EqualContents(iterator, end))
                {
                    height += Spacing + EditorGUI.GetPropertyHeight(iterator, true);
                    if (!iterator.NextVisible(false))
                        break;
                }
            }

            if (!string.IsNullOrEmpty(validationError))
                height += Spacing + ValidationHeight;

            return height;
        }

        public static void Draw(Rect rect, SerializedProperty property, GUIContent label, Type baseType, string summary, string validationError)
        {
            if (property.propertyType != SerializedPropertyType.ManagedReference)
            {
                EditorGUI.HelpBox(new Rect(rect.x, rect.y, rect.width, MissingAttributeHeight),
                    $"'{property.displayName}' needs [SerializeReference] to hold a {baseType.Name}.", MessageType.Error);
                return;
            }

            var header = new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight);
            var value = property.managedReferenceValue;

            if (value == null)
            {
                var assignRect = new Rect(header.xMax - AssignButtonWidth, header.y, AssignButtonWidth, header.height);
                EditorGUI.LabelField(new Rect(header.x, header.y, header.width - AssignButtonWidth - Spacing, header.height), label);
                if (GUI.Button(assignRect, _assignContent, EditorStyles.miniButton))
                    ShowPicker(assignRect, property, baseType);
                return;
            }

            var removeRect = new Rect(header.xMax - RemoveButtonWidth, header.y, RemoveButtonWidth, header.height);
            var changeRect = new Rect(removeRect.x - ChangeButtonWidth - Spacing, header.y, ChangeButtonWidth, header.height);
            var textWidth = changeRect.x - header.x - Spacing;

            property.isExpanded = EditorGUI.Foldout(new Rect(header.x, header.y, textWidth * 0.45f, header.height),
                property.isExpanded, label, true);

            _headerContent.text = string.IsNullOrEmpty(summary) ? NodeTypeInfo.For(value.GetType()).Name : summary;
            EditorGUI.LabelField(new Rect(header.x + textWidth * 0.45f, header.y, textWidth * 0.55f, header.height),
                _headerContent, HeaderStyle);

            if (GUI.Button(changeRect, _changeContent, EditorStyles.miniButton))
            {
                ShowPicker(changeRect, property, baseType);
                return;
            }

            if (GUI.Button(removeRect, _removeContent, EditorStyles.miniButton))
            {
                property.managedReferenceValue = null;
                return;
            }

            if (!property.isExpanded)
                return;

            var y = header.yMax;
            EditorGUI.indentLevel++;
            var iterator = property.Copy();
            var end = iterator.GetEndProperty();
            if (iterator.NextVisible(true))
            {
                while (!SerializedProperty.EqualContents(iterator, end))
                {
                    var height = EditorGUI.GetPropertyHeight(iterator, true);
                    y += Spacing;
                    EditorGUI.PropertyField(new Rect(rect.x, y, rect.width, height), iterator, true);
                    y += height;
                    if (!iterator.NextVisible(false))
                        break;
                }
            }

            EditorGUI.indentLevel--;

            if (!string.IsNullOrEmpty(validationError))
            {
                y += Spacing;
                EditorGUI.HelpBox(new Rect(rect.x, y, rect.width, ValidationHeight), validationError, MessageType.Warning);
            }
        }

        private static void ShowPicker(Rect activator, SerializedProperty property, Type baseType)
        {
            var target = property.serializedObject.targetObject;
            var graph = target as Graph;
            var path = property.propertyPath;
            TypeBrowserMenu.Show(activator, "Assign " + baseType.Name, baseType.Name, baseType,
                type => Assign(graph, target, path, type));
        }

        private static void Assign(Graph graph, UnityEngine.Object target, string path, Type type)
        {
            if (target == null)
            {
                Debug.LogError($"{nameof(PolymorphicReferenceField)}: the object owning '{path}' no longer exists, so the pick was dropped. " +
                               "Fix: reopen the graph window and assign again.");
                return;
            }

            if (type.GetConstructor(Type.EmptyTypes) == null)
            {
                Debug.LogError($"{nameof(PolymorphicReferenceField)}: type '{type.Name}' has no public parameterless constructor and cannot be assigned. " +
                               "Fix: add one — graph data is created by the editor and by the serializer.");
                return;
            }

            if (graph != null)
                MutationGuard.Notify(graph, "Assign " + NodeTypeInfo.For(type).Name);

            using (var serializedObject = new SerializedObject(target))
            {
                var property = serializedObject.FindProperty(path);
                if (property == null)
                {
                    Debug.LogError($"{nameof(PolymorphicReferenceField)}: property path '{path}' no longer resolves on '{target.name}', " +
                                   "so the pick was dropped. This happens when the selection changes while the browser is open. " +
                                   "Fix: reselect the element and assign again.");
                    return;
                }

                property.managedReferenceValue = Activator.CreateInstance(type);
                if (graph != null)
                    serializedObject.ApplyModifiedPropertiesWithoutUndo();
                else
                    serializedObject.ApplyModifiedProperties();
            }
        }
    }
}
