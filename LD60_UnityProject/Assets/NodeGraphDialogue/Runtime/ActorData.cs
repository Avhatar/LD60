using UnityEngine;

namespace NodeGraphAvr.Dialogue
{
    /// <summary>
    /// Stable identity of a dialogue participant. Dialogue nodes reference this asset
    /// directly, so the game receives the asset itself at playback and maps it to
    /// presentation however it wants. Grows extra data (portraits, colors) later
    /// without touching the graph format.
    /// </summary>
    [CreateAssetMenu(menuName = "NodeGraphAvr/Actor", fileName = "NewActor")]
    public class ActorData : ScriptableObject
    {
        [field: Tooltip("Name of this actor as shown to the player.")]
        [field: SerializeField]
        public string DisplayName { get; private set; } = "";
    }
}
