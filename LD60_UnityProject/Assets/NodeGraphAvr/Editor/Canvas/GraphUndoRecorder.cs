using System;
using UnityEditor;

namespace NodeGraphAvr.Editor
{
    /// <summary>
    /// Global editor-side undo seam: installs itself into MutationGuard so every
    /// graph mutation is recorded with Unity's undo system before state changes,
    /// then re-broadcasts the mutation to editor listeners such as window sessions
    /// tracking dirtiness. Also listens to Unity's property-modification stream so
    /// SerializedObject-based edits (inspector panel fields) reach the same Mutated
    /// event even though they record their own undo and bypass MutationGuard.
    /// </summary>
    public static class GraphUndoRecorder
    {
        public static event Action<Graph, string> Mutated;

        [InitializeOnLoadMethod]
        internal static void Install()
        {
            MutationGuard.BeforeMutation = Record;
            Undo.postprocessModifications -= OnPostprocessModifications;
            Undo.postprocessModifications += OnPostprocessModifications;
        }

        private static void Record(Graph graph, string operation)
        {
            Undo.RegisterCompleteObjectUndo(graph, operation);
            Mutated?.Invoke(graph, operation);
        }

        private static UndoPropertyModification[] OnPostprocessModifications(UndoPropertyModification[] modifications)
        {
            Graph notified = null;
            for (var i = 0; i < modifications.Length; i++)
            {
                var target = modifications[i].currentValue?.target as Graph;
                if (target == null || target == notified)
                    continue;

                notified = target;
                Mutated?.Invoke(target, "Edit Graph Fields");
            }

            return modifications;
        }
    }
}
