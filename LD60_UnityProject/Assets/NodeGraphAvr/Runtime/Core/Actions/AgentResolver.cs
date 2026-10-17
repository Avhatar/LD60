using System;
using UnityEngine;

namespace NodeGraphAvr
{
    internal struct AgentResolver
    {
        private GameObject _resolvedFor;
        private Component _component;

        public Component Component => _component;

        public bool Resolve(Type agentType, in ActionContext context, string ownerName)
        {
            if (agentType == null)
                return true;

            var agentObject = context.Agent;
            if (agentObject == null)
            {
                _resolvedFor = null;
                _component = null;
                Debug.LogError($"{ownerName}: needs an agent of type '{agentType.Name}' but the context carries no agent GameObject. " +
                               "The element reports failure and does nothing. Fix: supply an agent when starting the graph — " +
                               "bind the actor it belongs to, or pass the starting component's GameObject into the ActionContext.");
                return false;
            }

            if (ReferenceEquals(_resolvedFor, agentObject) && _component != null)
                return true;

            _resolvedFor = agentObject;
            _component = agentObject.GetComponent(agentType);
            if (_component == null)
            {
                Debug.LogError($"{ownerName}: agent '{agentObject.name}' has no component of type '{agentType.Name}', which this element requires. " +
                               "The element reports failure and does nothing. Fix: add that component to the agent, or assign an agent that has it.");
                return false;
            }

            return true;
        }
    }
}
