using UnityEngine;
using NodeGraphAvr.Dialogue;
using LD60.Dialogue;

namespace LD60.Space
{
    [CreateAssetMenu(fileName = "ActSpacecraft", menuName = "LD60/Act Spacecraft")]
    public class ActSpacecraftSO : GameDataSO
    {
        [field: Tooltip("Which spacecraft this entry places and configures.")]
        [field: SerializeField] public SpacecraftSO Spacecraft { get; private set; }

        [field: Tooltip("Body it sits on during this act. Leave empty for a spacecraft flying free, and fill the two fields below instead.")]
        [field: SerializeField] public CelestialBodySO Body { get; private set; }

        [field: Tooltip("Distance from the Sun during this act. Used only when Body is empty.")]
        [field: SerializeField] public float DistanceAu { get; private set; }

        [field: Tooltip("Heliocentric angle during this act. Used only when Body is empty.")]
        [field: SerializeField, Range(0f, 360f)] public float AngleDegrees { get; private set; }

        [field: Tooltip("Body it transmits to. Earth in almost every case. Never the body it sits on: that transmission starts and ends in the same place and only draws dots on top of it. Something parked on a body either stays silent and is reached another way, or transmits outward to a different body.")]
        [field: SerializeField] public CelestialBodySO TransmitTo { get; private set; }

        [field: Tooltip("Arc width of its transmissions, in degrees.")]
        [field: SerializeField] public float ArcWidthDegrees { get; private set; } = 10f;

        [field: Tooltip("Dots carried by one of its transmissions.")]
        [field: SerializeField] public int DotCount { get; private set; } = 12;

        [field: Tooltip("Travel speed of its transmissions. Deliberately slower than ours.")]
        [field: SerializeField] public float Speed { get; private set; } = 1.2f;

        [field: Tooltip("Seconds between its transmissions.")]
        [field: SerializeField] public float TransmitPeriodSeconds { get; private set; } = 18f;

        [field: Tooltip("Dots of its signal that have to be caught before it can be talked to directly.")]
        [field: SerializeField] public int DotsToCapture { get; private set; } = 8;

        [field: Tooltip("Dialogues this spacecraft can be overheard saying. One is picked at random each time its transmission is caught.")]
        [field: SerializeField] public DialogueGraph[] InterceptDialogues { get; private set; } = new DialogueGraph[0];

        public bool TransmitsOntoItself
        {
            get { return Body != null && TransmitTo == Body; }
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();

            if (!TransmitsOntoItself)
                return;

            Debug.LogError($"{nameof(ActSpacecraftSO)}: {nameof(TransmitTo)} is the same body as {nameof(Body)} on '{name}'. " +
                           "Required to differ, because a transmission that starts and ends on one body just draws dots on top of it. " +
                           $"Fix: aim {nameof(TransmitTo)} at a different body, or clear it and set {nameof(TransmitPeriodSeconds)} to zero to keep this one silent.");
        }
#endif
    }
}
