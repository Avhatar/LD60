using LD60.Dialogue;
using NodeGraphAvr.Dialogue;
using UnityEngine;

namespace LD60.Settings
{
    [CreateAssetMenu(fileName = "Ending", menuName = "LD60/Ending")]
    public class EndingSO : GameDataSO
    {
        [field: Tooltip("Played when every spacecraft of the act is on the record.")]
        [field: SerializeField] public DialogueGraph SuccessDialogue { get; private set; }

        [field: Tooltip("Played when the transmitter runs dry with senders still missing.")]
        [field: SerializeField] public DialogueGraph PowerFailureDialogue { get; private set; }

        [field: Tooltip("Played when the act reaches its final date with senders still missing.")]
        [field: SerializeField] public DialogueGraph TimeFailureDialogue { get; private set; }

        [field: Tooltip("Played after the player closes the final record. The last thing the game says.")]
        [field: SerializeField] public DialogueGraph FarewellDialogue { get; private set; }

        [field: Tooltip("Speaker every line of the farewell is put in the mouth of, whatever the graph says.")]
        [field: SerializeField] public SpacecraftSO FarewellSpeaker { get; private set; }

        [field: SerializeField, Range(0f, 10f)] public float FadeOutSeconds { get; private set; } = 1.5f;

        [field: SerializeField, Range(0f, 10f)] public float FadeInSeconds { get; private set; } = 1.5f;

        [field: SerializeField] public AnimationCurve FadeCurve { get; private set; } = new AnimationCurve(
            new Keyframe(0f, 0f, 0f, 2f),
            new Keyframe(1f, 1f, 0f, 0f));
    }
}
