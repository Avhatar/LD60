using System.Collections.Generic;
using System.IO;
using LD60.Settings;
using UnityEditor;
using UnityEngine;

namespace LD60.EditorTools
{
    public static class SoundContainerCreatorTool
    {
        private const string TargetFolder = "Assets/Data/Sound";
        private const string MenuPath = "Tools/Sound/Create SoundContainer From Selected &%m";

        [MenuItem(MenuPath)]
        private static void CreateSoundContainer()
        {
            List<AudioClip> clips = GetSelectedAudioClips();

            if (clips.Count == 0)
            {
                Debug.LogError($"{nameof(SoundContainerCreatorTool)}: no AudioClip selected in {nameof(CreateSoundContainer)}. " +
                               "This tool builds a SoundContainer out of the Project view selection, so it needs at least one AudioClip. " +
                               "Fix: select one or more AudioClip assets and retry (Alt+Ctrl+M).");
                return;
            }

            if (!AssetDatabase.IsValidFolder(TargetFolder))
            {
                Directory.CreateDirectory(TargetFolder);
                AssetDatabase.Refresh();
            }

            string baseName = $"SoundContainer_{StripVariantSuffix(clips[0].name)}.asset";
            string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{TargetFolder}/{baseName}");

            SoundContainerSO container = ScriptableObject.CreateInstance<SoundContainerSO>();
            AssetDatabase.CreateAsset(container, assetPath);

            SerializedObject serialized = new SerializedObject(container);
            SerializedProperty clipsProperty = serialized.FindProperty("<AudioClips>k__BackingField");
            if (clipsProperty == null)
            {
                Debug.LogError($"{nameof(SoundContainerCreatorTool)}: backing field for {nameof(SoundContainerSO.AudioClips)} not found in {nameof(CreateSoundContainer)}. " +
                               "The tool writes clips through SerializedObject because the property has a private setter. " +
                               "Fix: keep AudioClips declared as [field: SerializeField] on SoundContainerSO.");
                return;
            }

            clipsProperty.arraySize = clips.Count;
            for (int i = 0; i < clips.Count; i++)
                clipsProperty.GetArrayElementAtIndex(i).objectReferenceValue = clips[i];

            if (clips.Count > 1)
            {
                SerializedProperty randomProperty = serialized.FindProperty("<RandomSound>k__BackingField");
                if (randomProperty != null)
                    randomProperty.boolValue = true;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            SoundContainerSO created = AssetDatabase.LoadAssetAtPath<SoundContainerSO>(assetPath);
            EditorGUIUtility.PingObject(created);
            Selection.activeObject = created;

            Debug.Log($"{nameof(SoundContainerCreatorTool)}: created '{assetPath}' with {clips.Count} clip(s).");
        }

        [MenuItem(MenuPath, true)]
        private static bool CreateSoundContainerValidate()
        {
            Object[] selection = Selection.objects;
            for (int i = 0; i < selection.Length; i++)
            {
                if (selection[i] is AudioClip)
                    return true;
            }

            return false;
        }

        private static List<AudioClip> GetSelectedAudioClips()
        {
            List<AudioClip> result = new List<AudioClip>();
            Object[] selection = Selection.objects;
            for (int i = 0; i < selection.Length; i++)
            {
                AudioClip clip = selection[i] as AudioClip;
                if (clip != null)
                    result.Add(clip);
            }

            return result;
        }

        private static string StripVariantSuffix(string clipName)
        {
            int dash = clipName.LastIndexOf('-');
            if (dash <= 0 || dash == clipName.Length - 1)
                return clipName;

            for (int i = dash + 1; i < clipName.Length; i++)
            {
                if (!char.IsDigit(clipName[i]))
                    return clipName;
            }

            return clipName.Substring(0, dash);
        }
    }
}
