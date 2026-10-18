using LD60.Dialogue;
using LD60.Intro;
using UnityEngine;

namespace LD60.Settings
{
    [CreateAssetMenu(fileName = "Intro", menuName = "LD60/Intro")]
    public class IntroSO : GameDataSO
    {
        [field: SerializeField] public SpacecraftSO Narrator { get; private set; }

        [field: SerializeField, Range(0f, 20f)] public float StartDelaySeconds { get; private set; } = 5f;

        [field: SerializeField] public AnimationCurve FadeCurve { get; private set; } = new AnimationCurve(
            new Keyframe(0f, 0f, 0f, 2f),
            new Keyframe(1f, 1f, 0f, 0f));
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
