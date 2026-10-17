using UnityEditor;
using UnityEngine;

namespace NodeGraphAvr.Editor
{
    /// <summary>
    /// Property drawer for every [SerializeReference] field or list element holding a
    /// GraphAction, wherever it appears: on a node, inside a nested authoring class, or
    /// inside another action. All the work lives in PolymorphicReferenceField; this only
    /// supplies the base type, the action's self-description, and its validation line.
    /// </summary>
    [CustomPropertyDrawer(typeof(GraphAction), true)]
    public class GraphActionDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return PolymorphicReferenceField.GetHeight(property, ValidationOf(property));
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var action = ValueOf(property);
            PolymorphicReferenceField.Draw(position, property, label, typeof(GraphAction),
                action != null ? action.Summary : null, action != null ? action.Validate() : null);
        }

        private static string ValidationOf(SerializedProperty property)
        {
            var action = ValueOf(property);
            return action != null ? action.Validate() : null;
        }

        private static GraphAction ValueOf(SerializedProperty property)
        {
            if (property.propertyType != SerializedPropertyType.ManagedReference)
                return null;
            return property.managedReferenceValue as GraphAction;
        }
    }
}
