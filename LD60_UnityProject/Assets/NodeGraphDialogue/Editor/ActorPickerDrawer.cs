using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace NodeGraphAvr.Dialogue.Editor
{
    /// <summary>
    /// Draws an [ActorPicker] field as a popup fed by the owning dialogue graph's
    /// declared actors plus the dialogue system config's global ones. A current value
    /// missing from that union stays listed so it is never silently lost; outside a
    /// dialogue graph the field falls back to a plain object field.
    /// </summary>
    [CustomPropertyDrawer(typeof(ActorPickerAttribute))]
    public class ActorPickerDrawer : PropertyDrawer
    {
        private static readonly List<ActorData> _entries = new List<ActorData>();

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!(property.serializedObject.targetObject is DialogueGraph dialogueGraph))
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }

            var current = property.objectReferenceValue as ActorData;
            _entries.Clear();
            _entries.Add(null);
            foreach (var actor in dialogueGraph.Actors)
                if (actor != null && !_entries.Contains(actor))
                    _entries.Add(actor);
            var config = DialogueSystemConfigCache.Config;
            if (config != null)
                foreach (var actor in config.GlobalActors)
                    if (actor != null && !_entries.Contains(actor))
                        _entries.Add(actor);
            if (current != null && !_entries.Contains(current))
                _entries.Add(current);

            var labels = new GUIContent[_entries.Count];
            var currentIndex = 0;
            for (var i = 0; i < _entries.Count; i++)
            {
                labels[i] = _entries[i] == null ? new GUIContent("None") : new GUIContent(_entries[i].name);
                if (_entries[i] == current)
                    currentIndex = i;
            }

            EditorGUI.BeginProperty(position, label, property);
            var picked = EditorGUI.Popup(position, label, currentIndex, labels);
            if (picked != currentIndex)
                property.objectReferenceValue = _entries[picked];
            EditorGUI.EndProperty();
        }
    }
}
