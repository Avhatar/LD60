using LD60.Intro;
using UnityEngine;

namespace LD60.Settings
{
    [CreateAssetMenu(fileName = "Intro", menuName = "LD60/Intro")]
    public class IntroSO : GameDataSO
    {
        [field: SerializeField] public IntroStep[] Steps { get; private set; } = new IntroStep[0];

        public int StepCount
        {
            get { return Steps == null ? 0 : Steps.Length; }
        }

        public IntroStep GetStep(int index)
        {
            if (Steps == null || index < 0 || index >= Steps.Length)
                return null;

            return Steps[index];
        }
    }
}
