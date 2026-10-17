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
        [field: SerializeField] public AudioSO Audio { get; private set; }
        [field: SerializeField] public UiTextSO UiText { get; private set; }
    }
}
