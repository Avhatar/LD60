using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace NodeGraphAvr.Editor
{
    /// <summary>
    /// Owns the editing working copy of one graph asset: a graph deserialized from
    /// the asset's JSON into an unsaved ScriptableObject, independent of the imported
    /// object. Tracks dirtiness through the mutation guard and undo events, rebuilds
    /// runtime state after undo/redo, and saves by rewriting the JSON file and
    /// reimporting it. Created per window target; the owner must call Dispose.
    /// </summary>
    public class GraphWindowSession
    {
        public Graph WorkingCopy { get; private set; }

        public string AssetGuid { get; private set; }

        public bool IsDirty { get; private set; }

        public event Action Changed;

        private string _savedJson;

        private GraphWindowSession(string assetGuid, Graph workingCopy, string savedJson, bool isDirty)
        {
            AssetGuid = assetGuid;
            WorkingCopy = workingCopy;
            _savedJson = savedJson;
            IsDirty = isDirty;
            GraphUndoRecorder.Mutated += OnGraphMutated;
            Undo.undoRedoPerformed += OnUndoRedoPerformed;
        }

        public string AssetPath => AssetDatabase.GUIDToAssetPath(AssetGuid);

        public static GraphWindowSession Load(string assetGuid, string stashedJson = null, bool stashedDirty = false)
        {
            var path = AssetDatabase.GUIDToAssetPath(assetGuid);
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogError($"{nameof(GraphWindowSession)}: no asset path for guid '{assetGuid}' in {nameof(Load)}. " +
                               "The graph asset was deleted or the guid is stale. Fix: reopen the window from an existing graph asset.");
                return null;
            }

            string fileJson;
            try
            {
                fileJson = File.ReadAllText(path);
            }
            catch (IOException exception)
            {
                Debug.LogError($"{nameof(GraphWindowSession)}: could not read '{path}' in {nameof(Load)} — {exception.Message}. " +
                               "The file may be locked or deleted. Fix: restore the file, then reopen the window.");
                return null;
            }

            var hasStash = !string.IsNullOrEmpty(stashedJson);
            var workingCopy = GraphJsonSerializer.Deserialize(hasStash ? stashedJson : fileJson);
            if (workingCopy == null)
            {
                Debug.LogError($"{nameof(GraphWindowSession)}: could not build a working copy for '{path}' in {nameof(Load)}. " +
                               "See the previous error for the parse or format failure. Fix: repair the graph file, then reopen it.");
                return null;
            }

            workingCopy.name = Path.GetFileNameWithoutExtension(path);
            workingCopy.hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSave;
            var savedJson = hasStash ? fileJson : GraphJsonSerializer.Serialize(workingCopy);
            return new GraphWindowSession(assetGuid, workingCopy, savedJson, hasStash && stashedDirty);
        }

        public bool Save()
        {
            if (WorkingCopy == null)
            {
                Debug.LogError($"{nameof(GraphWindowSession)}: {nameof(WorkingCopy)} is null in {nameof(Save)}. " +
                               "The session was disposed or its graph was destroyed. Fix: reopen the graph asset.");
                return false;
            }

            var path = AssetPath;
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogError($"{nameof(GraphWindowSession)}: no asset path for guid '{AssetGuid}' in {nameof(Save)}. " +
                               "The graph asset was deleted while the window was open. Fix: recreate the asset, then use Save As via a new file.", WorkingCopy);
                return false;
            }

            string json;
            try
            {
                json = GraphJsonSerializer.Serialize(WorkingCopy);
            }
            catch (Exception exception)
            {
                Debug.LogError($"{nameof(GraphWindowSession)}: failed to serialize '{WorkingCopy.name}' in {nameof(Save)} — {exception.Message}", WorkingCopy);
                return false;
            }

            File.WriteAllText(path, json, new UTF8Encoding(false));
            AssetDatabase.ImportAsset(path);
            _savedJson = json;
            IsDirty = false;
            Changed?.Invoke();
            return true;
        }

        public void Dispose()
        {
            GraphUndoRecorder.Mutated -= OnGraphMutated;
            Undo.undoRedoPerformed -= OnUndoRedoPerformed;
            if (WorkingCopy != null)
                UnityEngine.Object.DestroyImmediate(WorkingCopy);
            WorkingCopy = null;
        }

        private void OnGraphMutated(Graph graph, string operation)
        {
            if (graph != WorkingCopy)
                return;

            IsDirty = true;
            Changed?.Invoke();
        }

        private void OnUndoRedoPerformed()
        {
            if (WorkingCopy == null)
            {
                Debug.LogError($"{nameof(GraphWindowSession)}: {nameof(WorkingCopy)} is null in {nameof(OnUndoRedoPerformed)}. " +
                               "The working copy was destroyed without disposing the session. Fix: dispose sessions through their owning window.");
                return;
            }

            WorkingCopy.RebuildRuntimeState();
            try
            {
                IsDirty = GraphJsonSerializer.Serialize(WorkingCopy) != _savedJson;
            }
            catch (Exception exception)
            {
                Debug.LogError($"{nameof(GraphWindowSession)}: could not re-serialize '{WorkingCopy.name}' after undo — {exception.Message}", WorkingCopy);
                IsDirty = true;
            }

            Changed?.Invoke();
        }
    }
}
