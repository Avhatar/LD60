using UnityEngine;

namespace NodeGraphAvr.Dialogue
{
    [CreateAssetMenu(menuName = "NodeGraphAvr/Actor", fileName = "NewActor")]
    public class ActorData : ScriptableObject
    {
        [field: Tooltip("Name of this actor as shown to the player.")]
        [field: SerializeField]
        public string DisplayName { get; private set; } = "";
    }
}
