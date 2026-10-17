using UnityEngine;

namespace LD60.Settings
{
    [CreateAssetMenu(fileName = "DialogueSettings", menuName = "LD60/Dialogue Settings")]
    public class DialogueSO : GameDataSO
    {
        [field: SerializeField, Range(1f, 200f)] public float TypewriterCharactersPerSecond { get; private set; } = 30f;
        [field: SerializeField] public SoundContainerSO TypewriterSound { get; private set; }

        [field: SerializeField] public Vector2 CaretSize { get; private set; } = new Vector2(12f, 20f);
        [field: SerializeField] public Vector2 CaretOffset { get; private set; } = new Vector2(6f, 0f);
        [field: SerializeField, Range(0.05f, 2f)] public float CaretBlinkSeconds { get; private set; } = 0.45f;

        [field: SerializeField, Range(0f, 2f)] public float BlurFadeSeconds { get; private set; } = 0.25f;
    }
}
