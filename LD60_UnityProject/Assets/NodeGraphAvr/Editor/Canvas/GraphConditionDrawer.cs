using UnityEditor;
using UnityEngine;

namespace NodeGraphAvr.Editor
{
    /// <summary>
    /// Property drawer for every [SerializeReference] field or list element holding a
    /// GraphCondition. Mirrors the action drawer — the shared polymorphic field does the
    /// drawing, this only supplies the base type, summary, and validation line.
    /// </summary>
    [CustomPropertyDrawer(typeof(GraphCondition), true)]
    public class GraphConditionDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return PolymorphicReferenceField.GetHeight(property, ValidationOf(property));
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var condition = ValueOf(property);
            PolymorphicReferenceField.Draw(position, property, label, typeof(GraphCondition),
                condition != null ? condition.Summary : null, condition != null ? condition.Validate() : null);
        }

        private static string ValidationOf(SerializedProperty property)
        {
            var condition = ValueOf(property);
            return condition != null ? condition.Validate() : null;
        }

        private static GraphCondition ValueOf(SerializedProperty property)
        {
            if (property.propertyType != SerializedPropertyType.ManagedReference)
                return null;
            return property.managedReferenceValue as GraphCondition;
        }
    }
}
