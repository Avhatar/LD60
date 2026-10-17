using UnityEngine;

namespace NodeGraphAvr.Dialogue
{
    /// <summary>
    /// Optional hook a game implements to answer "which scene object plays this actor?"
    /// for actors the dialogue was not told about explicitly — the protagonist, the
    /// current party, whoever the game computes on its own. The dialogue system holds no
    /// such policy: it asks, and falls back to the starting object when the answer is no.
    /// </summary>
    public interface IActorAgentSource
    {
        bool TryGetAgent(ActorData actor, out GameObject agent);
    }
}
