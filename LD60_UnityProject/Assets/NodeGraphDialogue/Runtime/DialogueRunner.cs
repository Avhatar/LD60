using System;
using System.Collections.Generic;
using UnityEngine;

namespace NodeGraphAvr.Dialogue
{
    /// <summary>
    /// Event-driven playback session over one DialogueGraph instance. Owns the cursor,
    /// the running flag, and the one action a node may be waiting on; the graph and its
    /// nodes stay pure data. Consumers subscribe to the per-instance request events and
    /// call the continuation carried by each request; there is no update pump. Following
    /// an out-connection index beyond the current node's children ends the dialogue in
    /// success — reaching a leaf is the normal happy ending. Actions and conditions run on
    /// the agent of the node's actor when one is bound, and on the starting object
    /// otherwise; deciding which scene object plays an actor is the game's business, not
    /// this class's.
    /// </summary>
    public class DialogueRunner : IActionCompletion
    {
        private DialogueGraph _graph;
        private GraphAction _runningAction;
        private ActionContext _defaultContext;
        private Dictionary<ActorData, GameObject> _actorAgents;
        private HashSet<ActorData> _warnedActors;
        private bool _awaitingAction;
        private int _pendingIndex;
        private int _runVersion;

        public event Action<SayRequest> SayRequested;
        public event Action<ChoiceRequest> ChoiceRequested;
        public event Action<bool> Finished;

        public bool IsRunning { get; private set; }

        public DialogueNode CurrentNode { get; private set; }

        public IActorAgentSource ActorAgentSource { get; set; }

        internal int RunVersion => _runVersion;

        public void Init(DialogueGraph graph)
        {
            Init(graph, default);
        }

        public void Init(DialogueGraph graph, ActionContext context)
        {
            if (graph == null)
            {
                Debug.LogError($"{nameof(DialogueRunner)}: {nameof(graph)} is null in {nameof(Init)}. " +
                               "The runner needs a graph instance to walk. " +
                               "Fix: instantiate the imported asset, call RebuildRuntimeState(), then Init.");
                return;
            }

            _graph = graph;
            _defaultContext = context;
        }

        public void BindActor(ActorData actor, GameObject agent)
        {
            if (actor == null)
            {
                Debug.LogError($"{nameof(DialogueRunner)}: {nameof(actor)} is null in {nameof(BindActor)}. " +
                               "A binding needs the actor asset it is for. " +
                               "Fix: pass the same ActorData that the graph's nodes select.");
                return;
            }

            _actorAgents ??= new Dictionary<ActorData, GameObject>();
            _actorAgents[actor] = agent;
            _warnedActors?.Remove(actor);
        }

        public void BindActors(IReadOnlyList<ActorAgentBinding> bindings)
        {
            if (bindings == null)
            {
                Debug.LogError($"{nameof(DialogueRunner)}: {nameof(bindings)} is null in {nameof(BindActors)}. " +
                               "Fix: pass an empty list instead of null.");
                return;
            }

            for (var i = 0; i < bindings.Count; i++)
            {
                var binding = bindings[i];
                if (binding == null || binding.Actor == null)
                    continue;
                BindActor(binding.Actor, binding.Agent);
            }
        }

        public void Play()
        {
            if (_graph == null)
            {
                Debug.LogError($"{nameof(DialogueRunner)}: no graph in {nameof(Play)}. " +
                               $"Fix: call {nameof(Init)} with a graph instance first.");
                return;
            }

            if (IsRunning)
            {
                Debug.LogError($"{nameof(DialogueRunner)}: '{_graph.name}' is already running in {nameof(Play)}. " +
                               "One runner plays one dialogue at a time. " +
                               $"Fix: wait for {nameof(Finished)} or call {nameof(Stop)} before replaying.");
                return;
            }

            var start = _graph.StartNode as DialogueNode;
            if (start == null)
            {
                Debug.LogError($"{nameof(DialogueRunner)}: '{_graph.name}' has no start node in {nameof(Play)}. " +
                               "An empty dialogue cannot play. Fix: author at least one node in the graph.");
                return;
            }

            _runVersion++;
            _warnedActors?.Clear();
            IsRunning = true;
            EnterNode(start);
        }

        public void Stop(bool success)
        {
            if (!IsRunning)
            {
                Debug.LogWarning($"{nameof(DialogueRunner)}: {nameof(Stop)} ignored — nothing is running. " +
                                 "Fix: only stop a dialogue that was started.");
                return;
            }

            Finish(success);
        }

        internal void ContinueTo(int index)
        {
            if (!IsRunning)
            {
                Debug.LogWarning($"{nameof(DialogueRunner)}: {nameof(ContinueTo)} ignored — the dialogue is not running.");
                return;
            }

            var outs = CurrentNode.OutConnections;
            if (index < 0 || index >= outs.Count)
            {
                Finish(true);
                return;
            }

            EnterNode((DialogueNode)outs[index].Target);
        }

        internal void FinishFromNode(bool success)
        {
            Finish(success);
        }

        internal void RaiseSay(SayRequest request)
        {
            if (SayRequested == null)
            {
                Debug.LogWarning($"{nameof(DialogueRunner)}: a say request on '{_graph.name}' has no subscriber; " +
                                 $"the dialogue stalls. Fix: subscribe to {nameof(SayRequested)} before {nameof(Play)}.");
                return;
            }

            SayRequested(request);
        }

        internal void RaiseChoice(ChoiceRequest request)
        {
            if (ChoiceRequested == null)
            {
                Debug.LogWarning($"{nameof(DialogueRunner)}: a choice request on '{_graph.name}' has no subscriber; " +
                                 $"the dialogue stalls. Fix: subscribe to {nameof(ChoiceRequested)} before {nameof(Play)}.");
                return;
            }

            ChoiceRequested(request);
        }

        internal void ContinueAfterAction(GraphAction action, int index)
        {
            if (!IsRunning)
            {
                Debug.LogWarning($"{nameof(DialogueRunner)}: an action was requested while the dialogue is not running; it was skipped.");
                return;
            }

            if (action == null)
            {
                ContinueTo(index);
                return;
            }

            _awaitingAction = true;
            _pendingIndex = index;
            _runningAction = action;
            action.Execute(ContextForNode(CurrentNode), this);
        }

        internal ActionContext ContextForNode(DialogueNode node)
        {
            var actor = node != null ? node.ActionActor : null;
            if (actor == null)
                return _defaultContext;

            var agent = ResolveActorAgent(actor, node);
            if (agent == null)
                return _defaultContext;

            return new ActionContext(agent, _defaultContext.CoroutineHost, _defaultContext.Sender);
        }

        void IActionCompletion.OnActionFinished(bool success)
        {
            if (!_awaitingAction)
                return;

            var index = _pendingIndex;
            _awaitingAction = false;
            _runningAction = null;

            if (!IsRunning)
                return;

            if (!success)
            {
                Debug.LogWarning($"{nameof(DialogueRunner)}: the action of node '{CurrentNode?.Name}' failed, " +
                                 "so the dialogue stops as failed. " +
                                 "Fix: check that node's action, or handle the failure before this point in the graph.");
                Finish(false);
                return;
            }

            ContinueTo(index);
        }

        private GameObject ResolveActorAgent(ActorData actor, DialogueNode node)
        {
            if (_actorAgents != null && _actorAgents.TryGetValue(actor, out var bound) && bound != null)
                return bound;

            if (ActorAgentSource != null && ActorAgentSource.TryGetAgent(actor, out var provided) && provided != null)
                return provided;

            _warnedActors ??= new HashSet<ActorData>();
            if (_warnedActors.Add(actor))
                Debug.LogWarning($"{nameof(DialogueRunner)}: actor '{actor.DisplayName}' is selected on node '{node?.Name}' " +
                                 "but no scene object plays it in this run, so its actions and conditions run on the starting object. " +
                                 $"Fix: call {nameof(BindActor)} for this actor before {nameof(Play)}, " +
                                 $"or set {nameof(ActorAgentSource)} so the game can resolve it.");
            return null;
        }

        private void EnterNode(DialogueNode node)
        {
            CurrentNode = node;
            node.Enter(this);
        }

        private void Finish(bool success)
        {
            _runVersion++;
            IsRunning = false;
            CurrentNode = null;
            _awaitingAction = false;
            var running = _runningAction;
            _runningAction = null;
            running?.Interrupt();
            Finished?.Invoke(success);
        }
    }
}
