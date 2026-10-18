using System.Collections.Generic;
using LD60.Dialogue;
using UnityEditor;
using UnityEngine;

namespace LD60.EditorTools
{
    [CustomEditor(typeof(VinilRoot))]
    public class VinilRootEditor : Editor
    {
        private const string SpacecraftFolder = "Assets/Data/Spacecraft";
        private const string RelatedSpacecraftField = "relatedSpacecraft";
        private const string UndoName = "Create Vinil Spacecraft Slots";

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();
            EditorGUI.BeginDisabledGroup(Application.isPlaying);

            if (GUILayout.Button("Create Missing Spacecraft Slots"))
                CreateMissingSlots((VinilRoot)target);

            EditorGUI.EndDisabledGroup();
        }

        private static void CreateMissingSlots(VinilRoot root)
        {
            if (!AssetDatabase.IsValidFolder(SpacecraftFolder))
            {
                Debug.LogError($"{nameof(VinilRootEditor)}: folder '{SpacecraftFolder}' does not exist in {nameof(CreateMissingSlots)}. " +
                               "Required as the list of spacecraft the record needs a slot for. " +
                               $"Fix: keep the {nameof(SpacecraftSO)} assets in that folder, or point {nameof(SpacecraftFolder)} at the folder they moved to.");
                return;
            }

            VinilSpacecraftSlotUi[] existing = root.GetComponentsInChildren<VinilSpacecraftSlotUi>(true);

            if (existing.Length == 0)
            {
                Debug.LogError($"{nameof(VinilRootEditor)}: no {nameof(VinilSpacecraftSlotUi)} found under '{root.name}' in {nameof(CreateMissingSlots)}. " +
                               "Required as the template every missing slot is copied from. " +
                               "Fix: build one slot by hand on the spacecraft side of the record and run this again.", root);
                return;
            }

            HashSet<SpacecraftSO> taken = new HashSet<SpacecraftSO>();

            for (int i = 0; i < existing.Length; i++)
            {
                SpacecraftSO craft = ReadRelatedSpacecraft(existing[i]);

                if (craft != null)
                    taken.Add(craft);
            }

            string[] guids = AssetDatabase.FindAssets("t:" + nameof(SpacecraftSO), new[] { SpacecraftFolder });
            string[] paths = new string[guids.Length];

            for (int i = 0; i < guids.Length; i++)
                paths[i] = AssetDatabase.GUIDToAssetPath(guids[i]);

            System.Array.Sort(paths);

            VinilSpacecraftSlotUi template = existing[0];
            Transform parent = template.transform.parent;
            int undoGroup = Undo.GetCurrentGroup();
            int created = 0;
            int kept = 0;

            for (int i = 0; i < paths.Length; i++)
            {
                SpacecraftSO craft = AssetDatabase.LoadAssetAtPath<SpacecraftSO>(paths[i]);

                if (craft == null)
                    continue;

                if (taken.Contains(craft))
                {
                    kept++;
                    continue;
                }

                if (!CreateSlot(template, parent, craft))
                    return;

                taken.Add(craft);
                created++;
            }

            root.SyncSlots();
            Undo.SetCurrentGroupName(UndoName);
            Undo.CollapseUndoOperations(undoGroup);

            Debug.Log($"{nameof(VinilRootEditor)}: created {created} slot(s) on '{root.name}', left {kept} that already had one.", root);
        }

        private static bool CreateSlot(VinilSpacecraftSlotUi template, Transform parent, SpacecraftSO craft)
        {
            VinilSpacecraftSlotUi slot = Object.Instantiate(template, parent);
            Undo.RegisterCreatedObjectUndo(slot.gameObject, UndoName);

            RectTransform rect = slot.transform as RectTransform;

            if (rect != null)
                rect.anchoredPosition3D = Vector3.zero;

            SerializedObject serialized = new SerializedObject(slot);
            SerializedProperty property = serialized.FindProperty(RelatedSpacecraftField);

            if (property == null)
            {
                Debug.LogError($"{nameof(VinilRootEditor)}: field '{RelatedSpacecraftField}' not found on {nameof(VinilSpacecraftSlotUi)} in {nameof(CreateSlot)}. " +
                               "The tool writes the spacecraft through SerializedObject because the field is private, so no slot can be filled in. " +
                               $"Fix: keep that field name on {nameof(VinilSpacecraftSlotUi)}, or rename {nameof(RelatedSpacecraftField)} in this tool to match.");
                Undo.DestroyObjectImmediate(slot.gameObject);
                return false;
            }

            property.objectReferenceValue = craft;
            serialized.ApplyModifiedProperties();
            slot.ApplyRelatedSpacecraft();
            return true;
        }

        private static SpacecraftSO ReadRelatedSpacecraft(VinilSpacecraftSlotUi slot)
        {
            SerializedObject serialized = new SerializedObject(slot);
            SerializedProperty property = serialized.FindProperty(RelatedSpacecraftField);

            if (property == null)
            {
                Debug.LogError($"{nameof(VinilRootEditor)}: field '{RelatedSpacecraftField}' not found on '{slot.name}' in {nameof(ReadRelatedSpacecraft)}. " +
                               "Required to tell which spacecraft already own a slot, so this one reads as free and would be duplicated. " +
                               $"Fix: keep that field name on {nameof(VinilSpacecraftSlotUi)}, or rename {nameof(RelatedSpacecraftField)} in this tool to match.", slot);
                return null;
            }

            return property.objectReferenceValue as SpacecraftSO;
        }
    }
}
