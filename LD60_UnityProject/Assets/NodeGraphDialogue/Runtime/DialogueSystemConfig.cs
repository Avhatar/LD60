using System.Collections.Generic;
using UnityEngine;

namespace NodeGraphAvr.Dialogue
{
    /// <summary>
    /// Project-wide dialogue settings asset. Declares the actors available in every
    /// dialogue (e.g. the player), merged into each graph's own actor list when the
    /// editor builds the speaker dropdown. The runtime never reads this asset —
    /// nodes hold direct actor references.
    /// </summary>
    [CreateAssetMenu(menuName = "NodeGraphAvr/Dialogue System Config", fileName = "DialogueSystemConfig")]
    public class DialogueSystemConfig : ScriptableObject
    {
        [Tooltip("Actors that may speak in any dialogue without being declared on the graph, e.g. the player.")]
        [SerializeField] private List<ActorData> _globalActors = new List<ActorData>();

        public IReadOnlyList<ActorData> GlobalActors => _globalActors;
    }
}
