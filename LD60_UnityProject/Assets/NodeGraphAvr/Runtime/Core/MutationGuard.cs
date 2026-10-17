using System;

namespace NodeGraphAvr
{
    /// <summary>
    /// Seam between the runtime mutation API and editor-side undo recording.
    /// Every Graph mutation notifies this guard before changing state; the editor
    /// assembly installs a recorder delegate at load time. The runtime assembly
    /// stays free of UnityEditor references.
    /// </summary>
    public static class MutationGuard
    {
        public static Action<Graph, string> BeforeMutation;

        internal static void Notify(Graph graph, string operation)
        {
            BeforeMutation?.Invoke(graph, operation);
        }
    }
}
