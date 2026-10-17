using UnityEditor;
using UnityEngine;
using LD60.Settings;

namespace LD60.EditorTools
{
    [CustomPropertyDrawer(typeof(LocalizedString))]
    public class LocalizedStringDrawer : PropertyDrawer
    {
        private const float TagWidth = 28f;
        private const float RowSpacing = 2f;
        private const int MaxLines = 6;

        private static readonly GUIContent _measured = new GUIContent();

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty ru = property.FindPropertyRelative("_ru");
            SerializedProperty en = property.FindPropertyRelative("_en");

            if (ru == null || en == null)
            {
                EditorGUI.PropertyField(position, property, label, true);
                return;
            }

            EditorGUI.BeginProperty(position, label, property);

            float labelWidth = string.IsNullOrEmpty(label.text) ? 0f : EditorGUIUtility.labelWidth;
            if (labelWidth > 0f)
                EditorGUI.LabelField(new Rect(position.x, position.y, labelWidth, EditorGUIUtility.singleLineHeight), label);

            float rowX = position.x + labelWidth;
            float rowWidth = position.width - labelWidth;
            float ruHeight = RowHeight(ru.stringValue, rowWidth - TagWidth);
            float enHeight = RowHeight(en.stringValue, rowWidth - TagWidth);

            DrawRow(new Rect(rowX, position.y, rowWidth, ruHeight), ru, Language.Ru);
            DrawRow(new Rect(rowX, position.y + ruHeight + RowSpacing, rowWidth, enHeight), en, Language.En);

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            SerializedProperty ru = property.FindPropertyRelative("_ru");
            SerializedProperty en = property.FindPropertyRelative("_en");

            if (ru == null || en == null)
                return EditorGUI.GetPropertyHeight(property, label, true);

            float width = EditorGUIUtility.currentViewWidth - EditorGUIUtility.labelWidth - TagWidth - 32f;
            return RowHeight(ru.stringValue, width) + RowSpacing + RowHeight(en.stringValue, width);
        }

        private static void DrawRow(Rect rect, SerializedProperty property, Language language)
        {
            Rect tagRect = new Rect(rect.x, rect.y, TagWidth, EditorGUIUtility.singleLineHeight);
            Rect textRect = new Rect(rect.x + TagWidth, rect.y, rect.width - TagWidth, rect.height);

            GUIStyle tagStyle = Localization.Current == language ? EditorStyles.boldLabel : EditorStyles.miniLabel;
            if (GUI.Button(tagRect, language == Language.Ru ? "RU" : "EN", tagStyle))
                Localization.SetLanguage(language);

            EditorGUI.BeginChangeCheck();
            string edited = EditorGUI.TextArea(textRect, property.stringValue, EditorStyles.textArea);
            if (EditorGUI.EndChangeCheck())
                property.stringValue = edited;
        }

        private static float RowHeight(string text, float width)
        {
            float singleLine = EditorGUIUtility.singleLineHeight;
            if (width <= 1f)
                return singleLine;

            _measured.text = string.IsNullOrEmpty(text) ? " " : text;
            return Mathf.Clamp(EditorStyles.textArea.CalcHeight(_measured, width), singleLine, singleLine * MaxLines);
        }
    }

    [CustomPropertyDrawer(typeof(GameDate))]
    public class GameDateDrawer : PropertyDrawer
    {
        private const float CellSpacing = 4f;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty year = property.FindPropertyRelative("_year");
            SerializedProperty month = property.FindPropertyRelative("_month");
            SerializedProperty day = property.FindPropertyRelative("_day");

            if (year == null || month == null || day == null)
            {
                EditorGUI.PropertyField(position, property, label, true);
                return;
            }

            EditorGUI.BeginProperty(position, label, property);

            Rect row = EditorGUI.PrefixLabel(position, label);
            float cell = (row.width - CellSpacing * 2f) / 3f;
            int indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            day.intValue = DrawCell(new Rect(row.x, row.y, cell, EditorGUIUtility.singleLineHeight), day.intValue, 1, 31);
            month.intValue = DrawCell(new Rect(row.x + cell + CellSpacing, row.y, cell, EditorGUIUtility.singleLineHeight), month.intValue, 1, 12);
            year.intValue = DrawCell(new Rect(row.x + (cell + CellSpacing) * 2f, row.y, cell, EditorGUIUtility.singleLineHeight), year.intValue, 1, 9999);

            EditorGUI.indentLevel = indent;
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }

        private static int DrawCell(Rect rect, int value, int min, int max)
        {
            return Mathf.Clamp(EditorGUI.IntField(rect, value), min, max);
        }
    }
}
