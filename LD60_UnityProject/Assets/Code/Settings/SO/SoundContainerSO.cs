using System.Collections.Generic;
using UnityEngine;

namespace LD60.Settings
{
    [CreateAssetMenu(fileName = "SoundContainer", menuName = "LD60/Sound Container")]
    public class SoundContainerSO : ScriptableObject
    {
        [field: SerializeField] public List<AudioClip> AudioClips { get; private set; } = new List<AudioClip>();

        [field: SerializeField, Range(0f, 1f)] public float Volume { get; private set; } = 1f;
        [field: SerializeField] public float VolumeVariance { get; private set; } = 0f;
        [field: SerializeField, Range(-3f, 3f)] public float Pitch { get; private set; } = 1f;
        [field: SerializeField] public float PitchVariance { get; private set; } = 0f;
        [field: SerializeField, Range(0f, 1f)] public float SpatialBlend { get; private set; } = 0f;
        [field: SerializeField, Range(-1f, 1f)] public float StereoPan { get; private set; } = 0f;
        [field: SerializeField] public bool Loop { get; private set; } = false;
        [field: SerializeField] public bool RandomSound { get; private set; } = false;
    }
}
