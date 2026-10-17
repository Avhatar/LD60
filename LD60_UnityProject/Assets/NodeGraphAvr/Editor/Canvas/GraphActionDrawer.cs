using UnityEditor;
using UnityEngine;

namespace NodeGraphAvr.Editor
{
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
