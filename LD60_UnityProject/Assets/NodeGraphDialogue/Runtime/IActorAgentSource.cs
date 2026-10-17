using UnityEngine;

namespace NodeGraphAvr.Dialogue
{
    public interface IActorAgentSource
    {
        bool TryGetAgent(ActorData actor, out GameObject agent);
    }
}
