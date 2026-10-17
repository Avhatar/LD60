using UnityEngine;

namespace LD60.Settings
{
    [CreateAssetMenu(fileName = "AudioSettings", menuName = "LD60/Audio Settings")]
    public class AudioSO : GameDataSO
    {
        [field: SerializeField, Range(1, 64)] public int Pool2DSize { get; private set; } = 16;
        [field: SerializeField, Range(0f, 0.2f)] public float TailFadeSeconds { get; private set; } = 0.02f;
        [field: SerializeField] public SoundContainerSO BackgroundMusic { get; private set; }
        [field: SerializeField] public float Default3DMinDistance { get; private set; } = 10f;
        [field: SerializeField] public float Default3DMaxDistance { get; private set; } = 150f;
        [field: SerializeField] public AudioRolloffMode Default3DRolloff { get; private set; } = AudioRolloffMode.Logarithmic;
    }
}
