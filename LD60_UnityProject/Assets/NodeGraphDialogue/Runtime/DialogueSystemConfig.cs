using System.Collections.Generic;
using UnityEngine;

namespace NodeGraphAvr.Dialogue
{
    [CreateAssetMenu(menuName = "NodeGraphAvr/Dialogue System Config", fileName = "DialogueSystemConfig")]
    public class DialogueSystemConfig : ScriptableObject
    {
        [Tooltip("Actors that may speak in any dialogue without being declared on the graph, e.g. the player.")]
        [SerializeField] private List<ActorData> _globalActors = new List<ActorData>();

        public IReadOnlyList<ActorData> GlobalActors => _globalActors;
    }
}
