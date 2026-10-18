using UnityEngine;
using LD60.Settings;
using LD60.Space;
using LD60.Signals;

namespace LD60
{
    [CreateAssetMenu(fileName = "Core", menuName = "LD60/Core")]
    public class CoreSO : GameDataSO
    {
        [field: SerializeField] public TimeScaleSO TimeScale { get; private set; }
        [field: SerializeField] public SolarSystemSO SolarSystem { get; private set; }
        [field: SerializeField] public SignalSO Signal { get; private set; }
        [field: SerializeField] public WattSO Watt { get; private set; }
        [field: SerializeField] public AudioSO Audio { get; private set; }
        [field: SerializeField] public UiTextSO UiText { get; private set; }
        [field: SerializeField] public TargetFrameSO TargetFrame { get; private set; }
        [field: SerializeField] public InterceptMarkerSO InterceptMarker { get; private set; }
        [field: SerializeField] public DialogueSO Dialogue { get; private set; }
        [field: SerializeField] public IntroSO Intro { get; private set; }
        [field: SerializeField] public bool IntroEnabled { get; private set; }
        [field: SerializeField] public EndingSO Ending { get; private set; }
        [field: SerializeField, Range(0.5f, 10f)] public float SkipHoldSeconds { get; private set; } = 3f;
        [field: SerializeField, Range(0f, 30f)] public float SkipHintFirstDelaySeconds { get; private set; } = 5f;
        [field: SerializeField, Range(0f, 120f)] public float LanguageHintSeconds { get; private set; } = 20f;
    }
}
