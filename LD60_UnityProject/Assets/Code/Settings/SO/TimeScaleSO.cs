using UnityEngine;

namespace LD60.Settings
{
    [CreateAssetMenu(fileName = "TimeScale", menuName = "LD60/Time Scale")]
    public class TimeScaleSO : GameDataSO
    {
        [field: SerializeField] public ActPeriod[] Acts { get; private set; } = new ActPeriod[0];
        [field: SerializeField] public int StartActIndex { get; private set; }
        [field: SerializeField] public float SignalTravelSecondsAtVoyager { get; private set; } = 8f;

        public int ActCount
        {
            get { return Acts == null ? 0 : Acts.Length; }
        }

        public ActPeriod GetAct(int index)
        {
            if (Acts == null || index < 0 || index >= Acts.Length)
                return null;

            return Acts[index];
        }
    }
}
