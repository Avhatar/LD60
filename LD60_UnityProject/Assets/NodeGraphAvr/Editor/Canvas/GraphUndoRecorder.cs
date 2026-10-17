using System;
using UnityEditor;

namespace NodeGraphAvr.Editor
{
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
