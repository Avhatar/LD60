using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace NodeGraphAvr.Editor
{
    [CustomEditor(typeof(NodeStyleConfig))]
    public class NodeStyleConfigEditor : UnityEditor.Editor
    {
        private const float DefaultSwatchWidth = 34f;
        private const float ColorFieldWidth = 68f;
        private const float RevertButtonWidth = 54f;
        private const float ClassNameWidth = 120f;
        private const float SwatchHeight = 14f;

        private class Row
        {
            public string Label;
            public string ClassName;
            public string SearchKey;
            public string TypeId;
            public Color DefaultColor;
        }

        private class Section
        {
            public string Title;
            public readonly List<Row> Rows = new List<Row>();
            public bool Expanded = true;
        }

        private readonly List<Section> _sections = new List<Section>();

        private NodeStyleConfig _config;
        private SerializedProperty _overridesProperty;
        private GUIStyle _overriddenLabelStyle;
        private string _search = "";
        private bool _duplicateConfigs;
        private int _pendingRemoval = -1;
        private bool _pendingRevertAll;

        private void OnEnable()
        {
            _config = (NodeStyleConfig)target;
            _overridesProperty = serializedObject.FindProperty("_overrides");
            _duplicateConfigs = AssetDatabase.FindAssets("t:" + nameof(NodeStyleConfig)).Length > 1;
            BuildRows();
            Undo.undoRedoPerformed += OnUndoRedo;
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedo;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            ApplyPendingChanges();
            _overriddenLabelStyle ??= new GUIStyle(EditorStyles.label) { fontStyle = FontStyle.Bold };

            EditorGUILayout.HelpBox(
                "Colors declared in code are the defaults; a type that declares none uses the tool's fallback color. " +
                "Change a row to override it for this project, revert a row to fall back to code. " +
                "Only overridden types are stored in this asset.",
                MessageType.Info);

            if (_duplicateConfigs)
                EditorGUILayout.HelpBox(
                    "This project holds more than one " + nameof(NodeStyleConfig) + " asset. The canvas uses whichever " +
                    "the asset search returns first. Fix: keep exactly one and delete the rest.",
                    MessageType.Warning);

            DrawToolbar();
            EditorGUILayout.Space(4f);

            for (var i = 0; i < _sections.Count; i++)
                DrawSection(_sections[i]);
            DrawUnknownEntries();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal();
            _search = EditorGUILayout.TextField("Search", _search);

            var count = _overridesProperty.arraySize;
            GUILayout.Label(count == 1 ? "1 override" : count + " overrides", EditorStyles.miniLabel, GUILayout.Width(80f));

            using (new EditorGUI.DisabledScope(count == 0))
            {
                if (GUILayout.Button("Revert All", GUILayout.Width(80f)))
                {
                    _pendingRevertAll = true;
                    Repaint();
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawSection(Section section)
        {
            var rows = section.Rows;
            var matches = 0;
            for (var i = 0; i < rows.Count; i++)
                if (Matches(rows[i]))
                    matches++;
            if (matches == 0)
                return;

            section.Expanded = EditorGUILayout.Foldout(section.Expanded, section.Title + "  (" + matches + ")", true, EditorStyles.foldoutHeader);
            if (!section.Expanded)
                return;

            EditorGUI.indentLevel++;
            for (var i = 0; i < rows.Count; i++)
            {
                if (!Matches(rows[i]))
                    continue;
                DrawRow(rows[i]);
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.Space(2f);
        }

        private void DrawRow(Row row)
        {
            var index = IndexOfOverride(row.TypeId);
            var overridden = index >= 0;
            var current = overridden
                ? _overridesProperty.GetArrayElementAtIndex(index).FindPropertyRelative("_color").colorValue
                : row.DefaultColor;

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(row.Label, overridden ? _overriddenLabelStyle : EditorStyles.label);
            GUILayout.Label(row.ClassName, EditorStyles.miniLabel, GUILayout.Width(ClassNameWidth));

            if (string.IsNullOrEmpty(row.TypeId))
            {
                GUILayout.Label("no [GraphTypeId]", EditorStyles.miniLabel,
                    GUILayout.Width(DefaultSwatchWidth + ColorFieldWidth + RevertButtonWidth + 8f));
                EditorGUILayout.EndHorizontal();
                return;
            }

            DrawDefaultSwatch(row.DefaultColor);

            var edited = EditorGUILayout.ColorField(GUIContent.none, current, true, false, false, GUILayout.Width(ColorFieldWidth));
            if (edited != current)
                SetOverride(row, index, edited);

            using (new EditorGUI.DisabledScope(!overridden))
            {
                if (GUILayout.Button("Revert", GUILayout.Width(RevertButtonWidth)) && overridden)
                {
                    _pendingRemoval = index;
                    Repaint();
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private static void DrawDefaultSwatch(Color color)
        {
            var rect = GUILayoutUtility.GetRect(DefaultSwatchWidth, SwatchHeight, GUILayout.Width(DefaultSwatchWidth));
            rect.y += 2f;
            rect.height = SwatchHeight - 2f;
            EditorGUI.DrawRect(rect, color);
        }

        private void DrawUnknownEntries()
        {
            var unknown = -1;
            for (var i = 0; i < _overridesProperty.arraySize; i++)
            {
                var id = _overridesProperty.GetArrayElementAtIndex(i).FindPropertyRelative("_typeId").stringValue;
                if (GraphTypeRegistry.TryGetType(id, out _))
                    continue;

                if (unknown < 0)
                {
                    EditorGUILayout.Space(4f);
                    EditorGUILayout.LabelField("Unknown types", EditorStyles.boldLabel);
                    EditorGUILayout.HelpBox(
                        "These entries name a [GraphTypeId] no type in the project claims — a renamed id, or a type from a " +
                        "system that is not in this project. They are kept so nothing is lost silently; remove one when it is " +
                        "genuinely obsolete.",
                        MessageType.None);
                    unknown = i;
                }

                EditorGUILayout.BeginHorizontal();
                GUILayout.Label(id, EditorStyles.label);
                if (GUILayout.Button("Remove", GUILayout.Width(70f)))
                {
                    _pendingRemoval = i;
                    Repaint();
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        private bool Matches(Row row)
        {
            if (string.IsNullOrEmpty(_search))
                return true;
            return row.SearchKey.IndexOf(_search, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private int IndexOfOverride(string typeId)
        {
            if (string.IsNullOrEmpty(typeId))
                return -1;

            var overrides = _config.Overrides;
            for (var i = 0; i < overrides.Count; i++)
                if (overrides[i] != null && overrides[i].TypeId == typeId)
                    return i;
            return -1;
        }

        private void SetOverride(Row row, int index, Color color)
        {
            if (index < 0)
            {
                index = _overridesProperty.arraySize;
                _overridesProperty.InsertArrayElementAtIndex(index);
                _overridesProperty.GetArrayElementAtIndex(index).FindPropertyRelative("_typeId").stringValue = row.TypeId;
            }

            _overridesProperty.GetArrayElementAtIndex(index).FindPropertyRelative("_color").colorValue = color;
            Apply();
        }

        private void ApplyPendingChanges()
        {
            if (_pendingRevertAll)
            {
                _pendingRevertAll = false;
                _overridesProperty.ClearArray();
                Apply();
                serializedObject.Update();
            }

            if (_pendingRemoval < 0)
                return;

            var index = _pendingRemoval;
            _pendingRemoval = -1;
            if (index >= _overridesProperty.arraySize)
                return;

            _overridesProperty.DeleteArrayElementAtIndex(index);
            Apply();
            serializedObject.Update();
        }

        private void Apply()
        {
            serializedObject.ApplyModifiedProperties();
            NodeStyleCache.Invalidate();
        }

        private void OnUndoRedo()
        {
            NodeStyleCache.Invalidate();
            Repaint();
        }

        private void BuildRows()
        {
            _sections.Clear();

            var kindBases = new List<Type>();
            var kindSections = new List<Section>();
            CollectGraphKinds(kindBases, kindSections);
            var otherNodes = new Section { Title = "Nodes - no graph kind claims them" };

            var nodeTypes = TypeCache.GetTypesDerivedFrom<Node>();
            for (var i = 0; i < nodeTypes.Count; i++)
            {
                var type = nodeTypes[i];
                if (!IsAuthorable(type))
                    continue;
                SectionForNode(type, kindBases, kindSections, otherNodes).Rows.Add(BuildRow(type));
            }

            kindSections.Sort(CompareSections);
            for (var i = 0; i < kindSections.Count; i++)
                if (kindSections[i].Rows.Count > 0)
                    _sections.Add(kindSections[i]);
            if (otherNodes.Rows.Count > 0)
                _sections.Add(otherNodes);

            _sections.Add(CollectSection("Actions", typeof(GraphAction)));
            _sections.Add(CollectSection("Conditions", typeof(GraphCondition)));

            for (var i = 0; i < _sections.Count; i++)
                _sections[i].Rows.Sort(CompareRows);
        }

        private static void CollectGraphKinds(List<Type> bases, List<Section> sections)
        {
            var graphTypes = TypeCache.GetTypesDerivedFrom<Graph>();
            for (var i = 0; i < graphTypes.Count; i++)
            {
                var graphType = graphTypes[i];
                if (!IsAuthorable(graphType))
                    continue;

                var probe = ScriptableObject.CreateInstance(graphType) as Graph;
                if (probe == null)
                    continue;

                var baseNodeType = probe.BaseNodeType;
                DestroyImmediate(probe);
                if (baseNodeType == null || bases.Contains(baseNodeType))
                    continue;

                bases.Add(baseNodeType);
                sections.Add(new Section { Title = "Nodes - " + ObjectNames.NicifyVariableName(graphType.Name) });
            }
        }

        private static Section SectionForNode(Type nodeType, List<Type> bases, List<Section> sections, Section fallback)
        {
            var best = -1;
            for (var i = 0; i < bases.Count; i++)
            {
                if (!bases[i].IsAssignableFrom(nodeType))
                    continue;
                if (best < 0 || bases[best].IsAssignableFrom(bases[i]))
                    best = i;
            }

            return best < 0 ? fallback : sections[best];
        }

        private static Section CollectSection(string title, Type baseType)
        {
            var section = new Section { Title = title };
            var types = TypeCache.GetTypesDerivedFrom(baseType);
            for (var i = 0; i < types.Count; i++)
                if (IsAuthorable(types[i]))
                    section.Rows.Add(BuildRow(types[i]));
            return section;
        }

        private static Row BuildRow(Type type)
        {
            var info = NodeTypeInfo.For(type);
            GraphTypeRegistry.TryGetId(type, out var typeId);
            return new Row
            {
                Label = info.Name,
                ClassName = type.Name,
                SearchKey = info.Name + " " + type.Name + " " + info.Category,
                TypeId = typeId,
                DefaultColor = info.Color,
            };
        }

        private static bool IsAuthorable(Type type)
        {
            if (type.IsAbstract || type.IsGenericTypeDefinition)
                return false;
            return !type.Assembly.GetName().Name.EndsWith(".Tests", StringComparison.Ordinal);
        }

        private static int CompareSections(Section left, Section right)
        {
            return string.Compare(left.Title, right.Title, StringComparison.OrdinalIgnoreCase);
        }

        private static int CompareRows(Row left, Row right)
        {
            var byLabel = string.Compare(left.Label, right.Label, StringComparison.OrdinalIgnoreCase);
            return byLabel != 0 ? byLabel : string.Compare(left.ClassName, right.ClassName, StringComparison.OrdinalIgnoreCase);
        }
    }
}
