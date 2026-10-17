using System;

namespace NodeGraphAvr
{
    public static class MutationGuard
    {
        public static Action<Graph, string> BeforeMutation;

        internal static void Notify(Graph graph, string operation)
        {
            BeforeMutation?.Invoke(graph, operation);
        }
    }
}
