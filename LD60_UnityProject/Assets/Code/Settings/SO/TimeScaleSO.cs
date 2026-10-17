using UnityEngine;

namespace LD60.Settings
{
    [CreateAssetMenu(fileName = "TimeScale", menuName = "LD60/Time Scale")]
    public class TimeScaleSO : GameDataSO
    {
        [field: SerializeField] public float SimulatedYearsPerRun { get; private set; } = 5f;
        [field: SerializeField] public float RunDurationSeconds { get; private set; } = 1200f;
        [field: SerializeField] public double DaysSinceJ2000AtRunStart { get; private set; } = 9786d;
        [field: SerializeField] public float SignalTravelSecondsAtVoyager { get; private set; } = 8f;

        public double DaysPerSecond
        {
            get
            {
                if (RunDurationSeconds <= 0f)
                    return 0d;

                return SimulatedYearsPerRun * 365.25d / RunDurationSeconds;
            }
        }
    }
}
